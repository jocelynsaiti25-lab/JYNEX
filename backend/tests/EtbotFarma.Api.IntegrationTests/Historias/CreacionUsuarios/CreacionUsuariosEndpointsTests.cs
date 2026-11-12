using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.CreacionUsuarios;

/// <summary>
/// HU-03 de punta a punta dentro de .NET:
/// Controller → MediatR → Validator → Handler → Repositorio → MySQL.
/// Se marcan con la categoría "Integracion" porque necesitan la base de datos.
/// </summary>
[Trait("Category", "Integracion")]
public class CreacionUsuariosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    /// <summary>Terminación distinta en cada ejecución para no chocar con usuarios de pruebas anteriores.</summary>
    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private static object DatosNuevos(string sufijo, int idRol = 2, string contrasenia = "Farmacia#2026") => new
    {
        nombre = "María",
        apellidos = "López",
        nombreUsuario = $"maria.{sufijo}",
        correo = $"maria.{sufijo}@etbotfarma.com",
        telefono = "5512345678",
        contrasenia,
        idRol
    };

    /// <summary>Inicia sesión y devuelve la cookie ("etbotfarma_sesion=...") para mandarla en las peticiones.</summary>
    private async Task<string> IniciarSesionAsync(string nombreUsuario, string contrasenia)
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario, contrasenia });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private async Task<HttpResponseMessage> CrearUsuarioAsync(object datos, string? cookie)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios") { Content = JsonContent.Create(datos) };
        if (cookie is not null)
        {
            peticion.Headers.Add("Cookie", cookie);
        }
        return await api.CrearCliente().SendAsync(peticion);
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var respuesta = await CrearUsuarioAsync(DatosNuevos(Unico()), cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_administrador_crea_la_cuenta_y_la_respuesta_no_trae_contrasenia()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();

        var respuesta = await CrearUsuarioAsync(DatosNuevos(sufijo), admin);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"maria.{sufijo}", json.GetProperty("nombreUsuario").GetString());
        Assert.Equal($"maria.{sufijo}@etbotfarma.com", json.GetProperty("correo").GetString());
        Assert.Equal("Empleado", json.GetProperty("rol").GetString());
        Assert.False(json.TryGetProperty("contrasenia", out _));
        Assert.False(json.TryGetProperty("contraseniaHash", out _));

        // Escenario 17: la cuenta nueva puede iniciar sesión
        await IniciarSesionAsync($"maria.{sufijo}", "Farmacia#2026");
    }

    /// <summary>
    /// Corrección de HU-03: al crear el usuario queda su auditoría en MySQL
    /// (quién = el admin con sesión, acción, usuario creado, fecha) y sin contraseña.
    /// </summary>
    [Fact]
    public async Task Al_crear_un_usuario_queda_su_registro_en_auditoria()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        Assert.Equal(HttpStatusCode.Created, (await CrearUsuarioAsync(DatosNuevos(sufijo), admin)).StatusCode);

        // Consulta directa a MySQL (solo en la prueba): auditoría del usuario recién creado
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync("""
            SELECT quien.nombre_usuario AS quien, a.accion, a.valor_anterior, a.valor_nuevo, a.fecha
            FROM auditoria a
            INNER JOIN usuario creado ON creado.id_usuario = a.id_registro
            INNER JOIN usuario quien ON quien.id_usuario = a.id_usuario
            WHERE a.tabla_afectada = 'usuario' AND creado.nombre_usuario = @nombreUsuario;
            """, CancellationToken.None);
        comando.Parameters.AddWithValue("@nombreUsuario", $"maria.{sufijo}");

        await using var lector = await comando.ExecuteReaderAsync();
        Assert.True(await lector.ReadAsync(), "No se encontró la auditoría del usuario creado.");
        Assert.Equal("admin", lector.GetString(lector.GetOrdinal("quien")));
        Assert.Equal("CREAR USUARIO", lector.GetString(lector.GetOrdinal("accion")));
        Assert.True(lector.IsDBNull(lector.GetOrdinal("valor_anterior")));
        var valorNuevo = lector.GetString(lector.GetOrdinal("valor_nuevo"));
        Assert.Contains($"maria.{sufijo}@etbotfarma.com", valorNuevo);
        Assert.DoesNotContain("Farmacia#2026", valorNuevo);
        Assert.True((DateTime.UtcNow - lector.GetDateTime(lector.GetOrdinal("fecha"))).Duration() < TimeSpan.FromMinutes(5));
        Assert.False(await lector.ReadAsync(), "Debe haber UN solo registro de auditoría por usuario creado.");
    }

    [Fact]
    public async Task Correo_ya_registrado_responde_422()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        Assert.Equal(HttpStatusCode.Created, (await CrearUsuarioAsync(DatosNuevos(sufijo), admin)).StatusCode);

        // Mismo correo (en mayúsculas), otro nombre de usuario
        var repetido = new
        {
            nombre = "Otra", apellidos = "Persona", nombreUsuario = $"otra.{sufijo}",
            correo = $"MARIA.{sufijo}@ETBOTFARMA.COM", telefono = "5512345678", contrasenia = "Farmacia#2026", idRol = 2
        };
        var respuesta = await CrearUsuarioAsync(repetido, admin);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("Este correo ya está registrado.", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Datos_invalidos_responden_400_con_los_errores_del_validator()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var datos = new
        {
            nombre = "María", apellidos = "López", nombreUsuario = $"maria.{Unico()}",
            correo = "maria.gmail", telefono = "55-12", contrasenia = "Farmacia#2026", idRol = 2
        };

        var respuesta = await CrearUsuarioAsync(datos, admin);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("Formato de correo inválido.", cuerpo);
        Assert.Contains("Formato de teléfono inválido", cuerpo);
    }

    [Fact]
    public async Task Un_empleado_no_puede_crear_usuarios_responde_403()
    {
        // 1. El administrador crea un empleado
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        Assert.Equal(HttpStatusCode.Created, (await CrearUsuarioAsync(DatosNuevos(sufijo), admin)).StatusCode);

        // 2. El empleado inicia sesión e intenta crear otra cuenta (criterio 1)
        var empleado = await IniciarSesionAsync($"maria.{sufijo}", "Farmacia#2026");
        var respuesta = await CrearUsuarioAsync(DatosNuevos(Unico()), empleado);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    /// <summary>Política de contraseñas: se aceptan 8 y 15 caracteres y con esa contraseña se puede entrar.</summary>
    [Theory]
    [InlineData("Farma#26")]          // 8 caracteres
    [InlineData("Farmacia#202601")]   // 15 caracteres
    public async Task Contrasenia_de_8_y_15_caracteres_crea_la_cuenta(string contrasenia)
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();

        var respuesta = await CrearUsuarioAsync(DatosNuevos(sufijo, contrasenia: contrasenia), admin);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        await IniciarSesionAsync($"maria.{sufijo}", contrasenia);
    }

    /// <summary>Política de contraseñas: si no se cumple responde 400 con el requisito que falta y no crea nada.</summary>
    [Theory]
    [InlineData("Fa#2026", "La contraseña debe tener al menos 8 caracteres.")]
    [InlineData("Farmacia#2026XYZ", "La contraseña no puede tener más de 15 caracteres.")]
    [InlineData("Farmacia2026", "La contraseña debe tener al menos un carácter especial")]
    public async Task Contrasenia_fuera_de_la_politica_responde_400(string contrasenia, string mensaje)
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();

        var respuesta = await CrearUsuarioAsync(DatosNuevos(sufijo, contrasenia: contrasenia), admin);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(mensaje, await respuesta.Content.ReadAsStringAsync());
        var noEntra = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar",
            new { nombreUsuario = $"maria.{sufijo}", contrasenia });
        Assert.NotEqual(HttpStatusCode.OK, noEntra.StatusCode);   // la cuenta no se creó
    }
}
