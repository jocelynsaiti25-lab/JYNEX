using System.Net;
using System.Net.Http.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.CierreSesion;

/// <summary>
/// HU-02 de punta a punta dentro de .NET:
/// Controller → MediatR → Handler → Repositorio → MySQL.
/// Se marcan con la categoría "Integracion" porque necesitan la base de datos.
/// </summary>
[Trait("Category", "Integracion")]
public class CierreSesionEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    /// <summary>Inicia sesión como admin y devuelve la cookie ("etbotfarma_sesion=...").</summary>
    private async Task<string> IniciarSesionAsync()
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync(
            "/api/sesiones/iniciar", new { nombreUsuario = "admin", contrasenia = ContraseniaAdmin });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    /// <summary>Envía una petición con (o sin) la cookie de sesión.</summary>
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

    private async Task<long> ContarUsuariosAsync(string nombreUsuario)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(
            "SELECT COUNT(*) FROM usuario WHERE nombre_usuario = @nombreUsuario;", CancellationToken.None);
        comando.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    /// <summary>Escenario 1: responde 204, borra la cookie del navegador y la sesión queda cerrada en MySQL.</summary>
    [Fact]
    public async Task Cerrar_sesion_responde_204_y_borra_la_cookie()
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", cookie);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var borrado = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("etbotfarma_sesion=;", borrado);                       // valor vacío
        Assert.Contains("expires=Thu, 01 Jan 1970", borrado, StringComparison.OrdinalIgnoreCase);   // fecha pasada = borrar
    }

    /// <summary>Escenarios 6 y 9: la cookie anterior ya no sirve para NINGUNA operación (otra pestaña, otra petición).</summary>
    [Fact]
    public async Task Despues_de_cerrar_la_cookie_anterior_responde_401()
    {
        var cookie = await IniciarSesionAsync();
        Assert.Equal(HttpStatusCode.OK, (await EnviarAsync(HttpMethod.Get, "/api/sesiones/actual", cookie)).StatusCode);

        await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", cookie);

        Assert.Equal(HttpStatusCode.Unauthorized, (await EnviarAsync(HttpMethod.Get, "/api/sesiones/actual", cookie)).StatusCode);
    }

    /// <summary>Escenario 6: con la cookie anterior el servidor rechaza un registro y NO guarda nada en MySQL.</summary>
    [Fact]
    public async Task Con_la_cookie_anterior_no_se_puede_registrar_nada()
    {
        var cookie = await IniciarSesionAsync();
        await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", cookie);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", cookie, new
        {
            nombre = "Luis",
            apellidos = "Pérez",
            nombreUsuario = $"luis.{sufijo}",
            correo = $"luis.{sufijo}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia = "Farmacia2026",
            idRol = 2
        });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(0, await ContarUsuariosAsync($"luis.{sufijo}"));
    }

    /// <summary>Escenario 10: si ya no hay sesión (expiró o ya se cerró), cerrar sesión NO da error.</summary>
    [Fact]
    public async Task Cerrar_sin_sesion_o_dos_veces_responde_204_sin_error()
    {
        var cookie = await IniciarSesionAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", cookie: null)).StatusCode);
    }

    /// <summary>Regla de negocio: las respuestas no se guardan en la caché del navegador (escenarios 3 y 4).</summary>
    [Fact]
    public async Task Las_respuestas_de_la_api_no_se_guardan_en_cache()
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/sesiones/actual", cookie);

        Assert.Contains("no-store", respuesta.Headers.CacheControl?.ToString());
    }

    /// <summary>Escenario 13: después de cerrar sesión, otra persona inicia con su cuenta y recibe una sesión NUEVA.</summary>
    [Fact]
    public async Task Despues_de_cerrar_un_nuevo_inicio_crea_otra_cookie_distinta()
    {
        var primera = await IniciarSesionAsync();
        await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", primera);

        var segunda = await IniciarSesionAsync();

        Assert.NotEqual(primera, segunda);
        Assert.Equal(HttpStatusCode.OK, (await EnviarAsync(HttpMethod.Get, "/api/sesiones/actual", segunda)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await EnviarAsync(HttpMethod.Get, "/api/sesiones/actual", primera)).StatusCode);
    }
}
