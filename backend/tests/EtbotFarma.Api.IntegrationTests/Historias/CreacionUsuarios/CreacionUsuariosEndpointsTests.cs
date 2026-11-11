using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    private static object DatosNuevos(string sufijo, int idRol = 2, string contrasenia = "Farmacia2026") => new
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
        await IniciarSesionAsync($"maria.{sufijo}", "Farmacia2026");
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
            correo = $"MARIA.{sufijo}@ETBOTFARMA.COM", telefono = "5512345678", contrasenia = "Farmacia2026", idRol = 2
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
            correo = "maria.gmail", telefono = "55-12", contrasenia = "Farmacia2026", idRol = 2
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
        var empleado = await IniciarSesionAsync($"maria.{sufijo}", "Farmacia2026");
        var respuesta = await CrearUsuarioAsync(DatosNuevos(Unico()), empleado);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}
