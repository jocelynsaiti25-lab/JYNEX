using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EtbotFarma.Api.IntegrationTests.Historias.MedicamentosAgotados;

[Trait("Category", "Integracion")]
public class MedicamentosAgotadosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private async Task<string> IniciarSesionAsync()
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar",
            new { nombreUsuario = "admin", contrasenia = ContraseniaAdmin });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, object? cuerpo, string? cookie)
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
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

    private async Task<int> CrearMedicamentoAsync(string cookie, string cantidadInicial)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/medicamentos", new
        {
            nombreComercial = $"Agotados {Unico()}",
            nombreSustancia = "Sustancia de prueba",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-INICIAL",
            fechaCaducidad = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(24).ToString("yyyy-MM-dd"),
            cantidadInicial
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("idMedicamento").GetInt32();
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/medicamentos/agotados", cuerpo: null, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Lista_un_medicamento_sin_existencias_y_excluye_los_que_si_tienen()
    {
        var cookie = await IniciarSesionAsync();
        var idAgotado = await CrearMedicamentoAsync(cookie, "0");
        var idConStock = await CrearMedicamentoAsync(cookie, "10");

        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/medicamentos/agotados", cuerpo: null, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var lista = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var ids = lista.EnumerateArray().Select(i => i.GetProperty("idMedicamento").GetInt32()).ToList();

        Assert.Contains(idAgotado, ids);
        Assert.DoesNotContain(idConStock, ids);
    }
}
