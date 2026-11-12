using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.ReasignacionContrasena;

/// <summary>
/// HU-05 de punta a punta dentro de .NET:
/// Controller → MediatR → Validator → Handler → Repositorios → MySQL (una sola transacción).
/// Se marcan con la categoría "Integracion" porque necesitan la base de datos.
/// Nunca se cambia la contraseña del "admin" (la usan las demás pruebas): se crean cuentas nuevas.
/// </summary>
[Trait("Category", "Integracion")]
public class ReasignacionContrasenaEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private const string Vieja = "Farmacia#2026";
    private const string Nueva = "Reasignada#2027";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private async Task<HttpResponseMessage> IniciarAsync(string nombreUsuario, string contrasenia)
        => await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario, contrasenia });

    private async Task<string> IniciarSesionAsync(string nombreUsuario, string contrasenia)
    {
        var respuesta = await IniciarAsync(nombreUsuario, contrasenia);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, string? cookie, object? cuerpo = null)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }
        if (cookie is not null)
        {
            peticion.Headers.Add("Cookie", cookie);
        }
        return await api.CrearCliente().SendAsync(peticion);
    }

    /// <summary>Crea una cuenta con HU-03 y devuelve (id, nombreUsuario).</summary>
    private async Task<(int Id, string NombreUsuario)> CrearCuentaAsync(string admin, int idRol = 2)
    {
        var nombreUsuario = $"reasignar.{Unico()}";
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", admin, new
        {
            nombre = "Ana",
            apellidos = "López",
            nombreUsuario,
            correo = $"{nombreUsuario}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia = Vieja,
            idRol
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("idUsuario").GetInt32(), nombreUsuario);
    }

    private Task<HttpResponseMessage> ReasignarAsync(int idUsuario, string? cookie, string nueva = Nueva, string? confirmar = null)
        => EnviarAsync(HttpMethod.Put, $"/api/usuarios/{idUsuario}/contrasenia", cookie,
            new { nuevaContrasenia = nueva, confirmarContrasenia = confirmar ?? nueva });

    private async Task<string> ConsultarAsync(string sql, int id)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", id);
        return Convert.ToString(await comando.ExecuteScalarAsync()) ?? string.Empty;
    }

    /// <summary>
    /// Ejecuta un UPDATE de preparación y lo CONFIRMA (COMMIT). ConexionMySql trabaja en transacción:
    /// sin GuardarCambiosAsync, al terminar hace ROLLBACK y el cambio se pierde.
    /// </summary>
    private async Task EjecutarAsync(string sql, int id)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using (var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None))
        {
            comando.Parameters.AddWithValue("@id", id);
            await comando.ExecuteNonQueryAsync();
        }
        await conexion.GuardarCambiosAsync(CancellationToken.None);
    }

    /// <summary>Escenarios 1, 12 y 13: cambia la contraseña, cierra la sesión anterior y solo sirve la nueva.</summary>
    [Fact]
    public async Task Reasignar_cambia_la_contrasenia_y_cierra_las_sesiones_anteriores()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var (id, usuario) = await CrearCuentaAsync(admin);
        var sesionAnterior = await IniciarSesionAsync(usuario, Vieja);   // la persona trabaja en otro equipo

        var respuesta = await ReasignarAsync(id, admin);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Nueva, json);   // nunca se devuelve la contraseña
        Assert.False((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eraSuPropiaCuenta").GetBoolean());

        // Escenario 12: la sesión anterior ya no sirve
        Assert.Equal(HttpStatusCode.Unauthorized, (await EnviarAsync(HttpMethod.Get, "/api/sesiones/actual", sesionAnterior)).StatusCode);

        // Escenario 13: con la anterior → mensaje genérico; con la nueva → entra con su rol
        var conVieja = await IniciarAsync(usuario, Vieja);
        Assert.Equal(HttpStatusCode.Unauthorized, conVieja.StatusCode);
        Assert.Contains("Usuario o contraseña incorrectos.", await conVieja.Content.ReadAsStringAsync());
        var conNueva = await IniciarAsync(usuario, Nueva);
        Assert.Equal(HttpStatusCode.OK, conNueva.StatusCode);
        Assert.Equal("Empleado", (await conNueva.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("rol").GetString());

        // Escenario 1: en MySQL se guarda un hash, no el texto
        var hash = await ConsultarAsync("SELECT contrasenia_hash FROM usuario WHERE id_usuario = @id;", id);
        Assert.NotEqual(Nueva, hash);
        Assert.DoesNotContain(Nueva, hash);
    }

    /// <summary>Escenario 14: auditoría con quién, a qué cuenta y cuándo, sin contraseña ni hash.</summary>
    [Fact]
    public async Task Reasignar_registra_la_auditoria_sin_contrasenia()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var (id, _) = await CrearCuentaAsync(admin);

        Assert.Equal(HttpStatusCode.OK, (await ReasignarAsync(id, admin)).StatusCode);

        Assert.Equal("1", await ConsultarAsync("SELECT COUNT(*) FROM auditoria WHERE accion = 'REASIGNAR CONTRASEÑA' AND tabla_afectada = 'usuario' AND id_registro = @id;", id));
        Assert.Equal("admin", await ConsultarAsync("""
            SELECT u.nombre_usuario FROM auditoria a JOIN usuario u ON u.id_usuario = a.id_usuario
            WHERE a.accion = 'REASIGNAR CONTRASEÑA' AND a.id_registro = @id;
            """, id));
        var valores = await ConsultarAsync("SELECT CONCAT(IFNULL(valor_anterior, ''), IFNULL(valor_nuevo, '')) FROM auditoria WHERE accion = 'REASIGNAR CONTRASEÑA' AND id_registro = @id;", id);
        var hash = await ConsultarAsync("SELECT contrasenia_hash FROM usuario WHERE id_usuario = @id;", id);
        Assert.DoesNotContain(Nueva, valores);
        Assert.DoesNotContain(hash, valores);
        var segundos = await ConsultarAsync("SELECT ABS(TIMESTAMPDIFF(SECOND, fecha, UTC_TIMESTAMP())) FROM auditoria WHERE accion = 'REASIGNAR CONTRASEÑA' AND id_registro = @id;", id);
        Assert.True(int.Parse(segundos) < 300);
    }

    /// <summary>Escenario 6: sin sesión (401) o Empleado (403) → no cambia ninguna contraseña.</summary>
    [Fact]
    public async Task Sin_permisos_no_cambia_ninguna_contrasenia()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var (id, usuario) = await CrearCuentaAsync(admin);
        var (_, empleado) = await CrearCuentaAsync(admin);
        var cookieEmpleado = await IniciarSesionAsync(empleado, Vieja);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ReasignarAsync(id, cookie: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ReasignarAsync(id, cookieEmpleado)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await EnviarAsync(HttpMethod.Get, $"/api/usuarios/{id}", cookieEmpleado)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await IniciarAsync(usuario, Vieja)).StatusCode);   // sigue la contraseña anterior
    }

    /// <summary>Escenario 7: cuenta que no existe → 404 "La cuenta no existe".</summary>
    [Fact]
    public async Task Con_una_cuenta_que_no_existe_responde_404()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);

        var put = await ReasignarAsync(999999, admin);
        var get = await EnviarAsync(HttpMethod.Get, "/api/usuarios/999999", admin);

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Contains("La cuenta no existe", await put.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    /// <summary>Escenario 8: cuenta desactivada → 422 "La cuenta está desactivada" y nada cambia.</summary>
    [Fact]
    public async Task Con_una_cuenta_desactivada_responde_422_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var (id, _) = await CrearCuentaAsync(admin);
        var hashAntes = await ConsultarAsync("SELECT contrasenia_hash FROM usuario WHERE id_usuario = @id;", id);
        await EjecutarAsync("UPDATE usuario SET activo = FALSE WHERE id_usuario = @id;", id);   // aún no existe la historia de desactivar

        var cuenta = await (await EnviarAsync(HttpMethod.Get, $"/api/usuarios/{id}", admin)).Content.ReadFromJsonAsync<JsonElement>();
        var respuesta = await ReasignarAsync(id, admin);

        Assert.False(cuenta.GetProperty("activo").GetBoolean());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("La cuenta está desactivada", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(hashAntes, await ConsultarAsync("SELECT contrasenia_hash FROM usuario WHERE id_usuario = @id;", id));
        Assert.Equal("0", await ConsultarAsync("SELECT COUNT(*) FROM auditoria WHERE accion = 'REASIGNAR CONTRASEÑA' AND id_registro = @id;", id));
    }

    /// <summary>Escenarios 9 y 10: política y coincidencia → 400 con el requisito que falta, sin cambios.</summary>
    [Theory]
    [InlineData("corta1A", "corta1A", "La contraseña debe tener al menos 8 caracteres.")]
    [InlineData("SinNumeroAqui", "SinNumeroAqui", "La contraseña debe tener al menos un número.")]
    [InlineData("Farmacia#2027XYZ", "Farmacia#2027XYZ", "La contraseña no puede tener más de 15 caracteres.")]
    [InlineData("SinEspecial2027", "SinEspecial2027", "La contraseña debe tener al menos un carácter especial")]
    [InlineData("Farmacia#2027", "Farmacia#2028", "Las contraseñas no coinciden")]
    [InlineData("", "", "La nueva contraseña es obligatoria.")]
    public async Task Datos_invalidos_responden_400_y_no_cambian_nada(string nueva, string confirmar, string mensaje)
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var (id, usuario) = await CrearCuentaAsync(admin);

        var respuesta = await ReasignarAsync(id, admin, nueva, confirmar);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(mensaje, await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await IniciarAsync(usuario, Vieja)).StatusCode);
    }

    /// <summary>Escenario 17: un Administrador cambia SU contraseña → se cierra también su sesión actual.</summary>
    [Fact]
    public async Task Si_el_administrador_cambia_su_propia_contrasenia_debe_volver_a_entrar()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var (idOtroAdmin, otroAdmin) = await CrearCuentaAsync(admin, idRol: 1);
        var cookieOtroAdmin = await IniciarSesionAsync(otroAdmin, Vieja);

        var respuesta = await ReasignarAsync(idOtroAdmin, cookieOtroAdmin);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eraSuPropiaCuenta").GetBoolean());
        Assert.StartsWith("etbotfarma_sesion=;", respuesta.Headers.GetValues("Set-Cookie").Single());   // cookie borrada
        Assert.Equal(HttpStatusCode.Unauthorized, (await EnviarAsync(HttpMethod.Get, "/api/sesiones/actual", cookieOtroAdmin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await IniciarAsync(otroAdmin, Nueva)).StatusCode);
    }

    /// <summary>Escenarios 2 y 4: los datos de la cuenta no traen la contraseña anterior ni su hash.</summary>
    [Fact]
    public async Task Los_datos_de_la_cuenta_no_traen_contrasenia_ni_hash()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var (id, usuario) = await CrearCuentaAsync(admin);

        var json = await (await EnviarAsync(HttpMethod.Get, $"/api/usuarios/{id}", admin)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(["idUsuario", "nombreCompleto", "nombreUsuario", "correo", "rol", "activo"], json.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal(usuario, json.GetProperty("nombreUsuario").GetString());
        Assert.Equal("Ana López", json.GetProperty("nombreCompleto").GetString());
        Assert.True(json.GetProperty("activo").GetBoolean());
    }
}
