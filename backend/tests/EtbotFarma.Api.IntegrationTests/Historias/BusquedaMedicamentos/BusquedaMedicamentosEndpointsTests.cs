using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EtbotFarma.Api.IntegrationTests.Historias.BusquedaMedicamentos;

[Trait("Category", "Integracion")]
public class BusquedaMedicamentosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private static string Fecha(int meses) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(meses).ToString("yyyy-MM-dd");

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

    private async Task<(int IdMedicamento, string Nombre)> CrearMedicamentoAsync(string cookie, string cantidadInicial)
    {
        var nombre = $"Busqueda {Unico()}";
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/medicamentos", new
        {
            nombreComercial = nombre,
            nombreSustancia = "Sustancia de prueba",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-INICIAL",
            fechaCaducidad = Fecha(24),
            cantidadInicial
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("idMedicamento").GetInt32(), nombre);
    }

    private async Task AgregarLoteAsync(string cookie, int idMedicamento, string numeroLote, string fecha, string cantidad)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/entradas-lote",
            new { idMedicamento, numeroLote, fechaCaducidad = fecha, cantidad }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    private async Task<JsonElement> ConsultarAsync(string cookie, string consulta)
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/inventario?{consulta}", cuerpo: null, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/inventario", cuerpo: null, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Devuelve_un_renglon_por_lote_ordenado_por_caducidad_con_su_semaforo()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, "10");
        await AgregarLoteAsync(cookie, id, "L-ROJO", Fecha(2), "5");

        var lista = await ConsultarAsync(cookie, $"termino={Uri.EscapeDataString(nombre)}");

        Assert.Equal(2, lista.GetArrayLength());
        Assert.Equal("L-ROJO", lista[0].GetProperty("numeroLote").GetString());
        Assert.Equal("Rojo", lista[0].GetProperty("semaforoCaducidad").GetString());
        Assert.Equal("L-INICIAL", lista[1].GetProperty("numeroLote").GetString());
        Assert.Equal("Verde", lista[1].GetProperty("semaforoCaducidad").GetString());
        Assert.Equal(5, lista[0].GetProperty("existencias").GetInt32());
        Assert.Equal("Disponible", lista[0].GetProperty("estadoDisponibilidad").GetString());
    }

    [Fact]
    public async Task La_busqueda_no_distingue_mayusculas_y_tambien_busca_por_lote()
    {
        var cookie = await IniciarSesionAsync();
        var (_, nombre) = await CrearMedicamentoAsync(cookie, "10");

        var porNombre = await ConsultarAsync(cookie, $"termino={Uri.EscapeDataString(nombre.ToUpperInvariant())}");

        Assert.Equal(1, porNombre.GetArrayLength());
        Assert.Equal(nombre, porNombre[0].GetProperty("nombreComercial").GetString());
    }

    [Fact]
    public async Task El_filtro_de_semaforo_se_combina_con_el_texto()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, "10");
        await AgregarLoteAsync(cookie, id, "L-ROJO", Fecha(2), "5");

        var rojos = await ConsultarAsync(cookie, $"termino={Uri.EscapeDataString(nombre)}&semaforo=Rojo");
        var verdes = await ConsultarAsync(cookie, $"termino={Uri.EscapeDataString(nombre)}&semaforo=Verde");

        Assert.Equal(1, rojos.GetArrayLength());
        Assert.Equal("L-ROJO", rojos[0].GetProperty("numeroLote").GetString());
        Assert.Equal(1, verdes.GetArrayLength());
        Assert.Equal("L-INICIAL", verdes[0].GetProperty("numeroLote").GetString());
    }

    [Fact]
    public async Task Un_medicamento_sin_unidades_aparece_como_agotado_sin_lote()
    {
        var cookie = await IniciarSesionAsync();
        var (_, nombre) = await CrearMedicamentoAsync(cookie, "0");

        var lista = await ConsultarAsync(cookie, $"termino={Uri.EscapeDataString(nombre)}");

        Assert.Equal(1, lista.GetArrayLength());
        Assert.Equal("Sin lote", lista[0].GetProperty("numeroLote").GetString());
        Assert.Equal(0, lista[0].GetProperty("existencias").GetInt32());
        Assert.Equal("SinStock", lista[0].GetProperty("semaforoCaducidad").GetString());
        Assert.Equal("Agotado", lista[0].GetProperty("estadoDisponibilidad").GetString());
    }

    [Fact]
    public async Task Sin_coincidencias_responde_200_con_lista_vacia()
    {
        var cookie = await IniciarSesionAsync();

        var lista = await ConsultarAsync(cookie, $"termino=NoExiste{Unico()}");

        Assert.Equal(0, lista.GetArrayLength());
    }
}
