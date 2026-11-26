using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EtbotFarma.Api.IntegrationTests.Historias.StockMinimo;

[Trait("Category", "Integracion")]
public class StockMinimoEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
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

    private async Task<int> CrearMedicamentoAsync(string cookie)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/medicamentos", new
        {
            nombreComercial = $"Stock {Unico()}",
            nombreSustancia = "Sustancia de prueba",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-INICIAL",
            fechaCaducidad = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(24).ToString("yyyy-MM-dd"),
            cantidadInicial = "10"
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("idMedicamento").GetInt32();
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/medicamentos/stock-minimo", cuerpo: null, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Actualiza_el_stock_minimo_y_aparece_en_la_lista()
    {
        var cookie = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(cookie);

        var actualizacion = await EnviarAsync(
            HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = 7 }, cookie);
        Assert.Equal(HttpStatusCode.OK, actualizacion.StatusCode);
        var cuerpoActualizacion = await actualizacion.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, cuerpoActualizacion.GetProperty("stockMinimo").GetInt32());

        var lista = await EnviarAsync(HttpMethod.Get, "/api/medicamentos/stock-minimo", cuerpo: null, cookie);
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var items = await lista.Content.ReadFromJsonAsync<JsonElement>();
        var item = items.EnumerateArray().Single(i => i.GetProperty("idMedicamento").GetInt32() == idMedicamento);
        Assert.Equal(7, item.GetProperty("stockMinimo").GetInt32());
        Assert.Equal(10, item.GetProperty("existenciasVigentes").GetInt32());
        Assert.False(item.GetProperty("enStockBajo").GetBoolean());
    }

    [Fact]
    public async Task Valor_negativo_responde_400()
    {
        var cookie = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(cookie);

        var respuesta = await EnviarAsync(
            HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = -1 }, cookie);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Medicamento_inexistente_responde_404()
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await EnviarAsync(
            HttpMethod.Put, "/api/medicamentos/999999/stock-minimo", new { nuevoStockMinimo = 5 }, cookie);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
