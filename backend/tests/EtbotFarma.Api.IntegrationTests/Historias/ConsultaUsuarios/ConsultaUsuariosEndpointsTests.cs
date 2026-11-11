using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EtbotFarma.Api.IntegrationTests.Historias.ConsultaUsuarios;

/// <summary>
/// HU-04 de punta a punta dentro de .NET:
/// Controller → MediatR → Validator → Handler → Repositorio → MySQL.
/// Se marcan con la categoría "Integracion" porque necesitan la base de datos.
/// </summary>
[Trait("Category", "Integracion")]
public class ConsultaUsuariosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    /// <summary>Terminación distinta en cada ejecución para no chocar con usuarios de pruebas anteriores.</summary>
    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private async Task<string> IniciarSesionAsync(string nombreUsuario, string contrasenia)
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario, contrasenia });
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

    /// <summary>Crea una cuenta con HU-03 (como lo haría el administrador) y devuelve su nombre de usuario.</summary>
    private async Task<string> CrearCuentaAsync(string admin, string nombreUsuario, int idRol = 2, string nombre = "María", string apellidos = "Fernández López")
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", admin, new
        {
            nombre,
            apellidos,
            nombreUsuario,
            correo = $"{nombreUsuario}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia = "Farmacia2026",
            idRol
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return nombreUsuario;
    }

    private async Task<JsonElement[]> ConsultarAsync(string cookie, string? busqueda = null)
    {
        var url = busqueda is null ? "/api/usuarios" : $"/api/usuarios?busqueda={Uri.EscapeDataString(busqueda)}";
        var respuesta = await EnviarAsync(HttpMethod.Get, url, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement[]>())!;
    }

    /// <summary>Escenarios 1 y 8: el Administrador ve la cuenta recién creada con todos sus datos y rol.</summary>
    [Fact]
    public async Task El_administrador_ve_la_lista_con_la_cuenta_recien_creada()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var nombreUsuario = await CrearCuentaAsync(admin, $"mflopez.{Unico()}");

        var lista = await ConsultarAsync(admin);

        var cuenta = Assert.Single(lista, u => u.GetProperty("nombreUsuario").GetString() == nombreUsuario);
        Assert.Equal("María Fernández López", cuenta.GetProperty("nombreCompleto").GetString());
        Assert.Equal($"{nombreUsuario}@etbotfarma.com", cuenta.GetProperty("correo").GetString());
        Assert.Equal("5512345678", cuenta.GetProperty("telefono").GetString());
        Assert.Equal("Empleado", cuenta.GetProperty("rol").GetString());
        Assert.Contains(lista, u => u.GetProperty("nombreUsuario").GetString() == "admin");
    }

    /// <summary>Escenario 7: la respuesta de la API no trae contraseñas ni hashes.</summary>
    [Fact]
    public async Task La_respuesta_no_trae_contrasenias_ni_hashes()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        await CrearCuentaAsync(admin, $"seguridad.{Unico()}");

        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/usuarios", admin);
        var json = await respuesta.Content.ReadAsStringAsync();

        // Cada cuenta trae EXACTAMENTE estos 6 campos: ni uno más (no hay forma de que se cuele el hash)
        string[] esperados = ["idUsuario", "nombreCompleto", "nombreUsuario", "correo", "telefono", "rol"];
        foreach (var cuenta in JsonSerializer.Deserialize<JsonElement[]>(json)!)
        {
            Assert.Equal(esperados, cuenta.EnumerateObject().Select(p => p.Name).ToArray());
        }
        Assert.DoesNotContain("Farmacia2026", json);   // la contraseña en texto
        Assert.DoesNotContain("AQAAAA", json);         // inicio de los hashes PBKDF2 de ASP.NET Identity
    }

    /// <summary>Escenario 3: sin sesión → 401 y ningún dato.</summary>
    [Fact]
    public async Task Sin_sesion_responde_401_sin_datos()
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/usuarios", cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.DoesNotContain("nombreUsuario", await respuesta.Content.ReadAsStringAsync());
    }

    /// <summary>Escenario 3: un Empleado → 403 y ningún dato.</summary>
    [Fact]
    public async Task Un_empleado_recibe_403_sin_datos()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var empleado = await CrearCuentaAsync(admin, $"empleado.{Unico()}");
        var cookieEmpleado = await IniciarSesionAsync(empleado, "Farmacia2026");

        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/usuarios", cookieEmpleado);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.DoesNotContain("nombreUsuario", await respuesta.Content.ReadAsStringAsync());
    }

    /// <summary>Escenarios 4 y 10: coincidencia parcial, sin importar mayúsculas ni espacios sobrantes.</summary>
    [Fact]
    public async Task La_busqueda_es_parcial_e_ignora_mayusculas_y_espacios()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        var buscado = await CrearCuentaAsync(admin, $"mflopez{sufijo}");
        await CrearCuentaAsync(admin, $"otro{sufijo}", nombre: "Luis", apellidos: "Pérez");

        var lista = await ConsultarAsync(admin, $"  LOPEZ{sufijo.ToUpperInvariant()}  ");

        Assert.Equal(buscado, Assert.Single(lista).GetProperty("nombreUsuario").GetString());
    }

    /// <summary>Escenario 5: sin coincidencias → lista vacía (200, no error).</summary>
    [Fact]
    public async Task Sin_coincidencias_responde_lista_vacia()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);

        var lista = await ConsultarAsync(admin, $"noexiste{Unico()}");

        Assert.Empty(lista);
    }

    /// <summary>
    /// Seguridad: "%" y "_" se buscan como texto (no como comodines de LIKE) y un intento de
    /// inyección SQL no devuelve datos ni rompe la tabla.
    /// </summary>
    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("' OR '1'='1")]
    public async Task Los_caracteres_especiales_se_buscan_como_texto(string busqueda)
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);

        var lista = await ConsultarAsync(admin, busqueda);

        Assert.DoesNotContain(lista, u => !u.GetProperty("nombreUsuario").GetString()!.Contains(busqueda));
        Assert.Contains((await ConsultarAsync(admin)), u => u.GetProperty("nombreUsuario").GetString() == "admin");
    }

    /// <summary>Una búsqueda de más de 50 caracteres la rechaza el validator (400).</summary>
    [Fact]
    public async Task Una_busqueda_muy_larga_responde_400()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);

        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/usuarios?busqueda={new string('a', 51)}", admin);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("La búsqueda no puede tener más de 50 caracteres.", await respuesta.Content.ReadAsStringAsync());
    }
}
