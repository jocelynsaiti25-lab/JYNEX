using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EtbotFarma.Api.IntegrationTests.Historias.InicioSesion;

/// <summary>
/// HU-01 de punta a punta dentro de .NET:
/// Controller → MediatR → Validator → Handler → Repositorio → MySQL.
/// Se marcan con la categoría "Integracion" porque necesitan la base de datos.
/// </summary>
[Trait("Category", "Integracion")]
public class InicioSesionEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private HttpClient CrearCliente() => api.CrearCliente();

    [Fact]
    public async Task Estado_del_sistema_responde_200()
    {
        var respuesta = await CrearCliente().GetAsync("/api/sistema/estado");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_datos_responde_400_con_los_errores_del_validator()
    {
        var respuesta = await CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario = "", contrasenia = "" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("El nombre de usuario es obligatorio.", cuerpo);
        Assert.Contains("La contraseña es obligatoria.", cuerpo);
    }

    [Fact]
    public async Task Contrasenia_incorrecta_responde_401_con_mensaje_generico()
    {
        var respuesta = await CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario = "admin", contrasenia = "Equivocada1" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains("Usuario o contraseña incorrectos.", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sin_sesion_la_sesion_actual_responde_401()
    {
        var respuesta = await CrearCliente().GetAsync("/api/sesiones/actual");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Credenciales_correctas_crean_cookie_segura_y_la_sesion_actual_responde_200()
    {
        var cliente = CrearCliente();

        // 1. Iniciar sesión
        var respuesta = await cliente.PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario = "admin", contrasenia = ContraseniaAdmin });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        // 2. La cookie es HttpOnly + Secure + SameSite=Strict y el token NO viene en el JSON
        var cookie = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("etbotfarma_sesion=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin", json.GetProperty("nombreUsuario").GetString());
        Assert.False(json.TryGetProperty("token", out _));

        // 3. Enviando esa cookie, la sesión actual responde 200 con el mismo usuario
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/sesiones/actual");
        peticion.Headers.Add("Cookie", cookie.Split(';')[0]);
        var actual = await cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
        var usuario = await actual.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin", usuario.GetProperty("nombreUsuario").GetString());
    }
}
