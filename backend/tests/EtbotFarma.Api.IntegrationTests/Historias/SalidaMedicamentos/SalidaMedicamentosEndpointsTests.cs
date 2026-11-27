using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.SalidaMedicamentos;

[Trait("Category", "Integracion")]
public class SalidaMedicamentosEndpointsTests(ApiConMySql api, ApiConBitacoraDeSalidaQueFalla apiFalla)
    : IClassFixture<ApiConMySql>, IClassFixture<ApiConBitacoraDeSalidaQueFalla>
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

    private static async Task<HttpResponseMessage> EnviarAsync(
        HttpClient cliente, HttpMethod metodo, string ruta, object? cuerpo, string? cookie)
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
        return await cliente.SendAsync(peticion);
    }

    private async Task<(int Id, string Nombre)> CrearMedicamentoAsync(string cookie, string cantidadInicial)
    {
        var nombre = $"Salida {Unico()}";
        var respuesta = await EnviarAsync(api.CrearCliente(), HttpMethod.Post, "/api/medicamentos", new
        {
            nombreComercial = nombre,
            nombreSustancia = "Sustancia de prueba",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-LARGO",
            fechaCaducidad = Fecha(24),
            cantidadInicial
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("idMedicamento").GetInt32(), nombre);
    }

    private async Task AgregarLoteAsync(string cookie, int idMedicamento, string numeroLote, int mesesCaducidad, string cantidad)
    {
        var respuesta = await EnviarAsync(api.CrearCliente(), HttpMethod.Post, "/api/entradas-lote", new
        {
            idMedicamento,
            numeroLote,
            fechaCaducidad = Fecha(mesesCaducidad),
            cantidad
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    private async Task<string> ConsultarAsync(string sql, int idMedicamento)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", idMedicamento);
        return Convert.ToString(await comando.ExecuteScalarAsync()) ?? string.Empty;
    }

    private Task<string> StockDeAsync(int idMedicamento, string numeroLote) =>
        ConsultarAsync(
            $"SELECT stock FROM lote WHERE id_medicamento = @id AND numero_lote = '{numeroLote}';", idMedicamento);

    private Task<string> SalidasDeAsync(int idMedicamento) =>
        ConsultarAsync("SELECT COUNT(*) FROM movimiento WHERE tipo = 'SALIDA' AND id_medicamento = @id;", idMedicamento);

    private Task<HttpResponseMessage> RegistrarAsync(string cookie, int idMedicamento, string cantidad, HttpClient? cliente = null) =>
        EnviarAsync(cliente ?? api.CrearCliente(), HttpMethod.Post, "/api/salidas-medicamento",
            new { idMedicamento, cantidad }, cookie);

    [Theory]
    [InlineData("GET", "/api/salidas-medicamento/medicamentos")]
    [InlineData("GET", "/api/salidas-medicamento/propuesta?idMedicamento=1&cantidad=1")]
    [InlineData("POST", "/api/salidas-medicamento")]
    public async Task Sin_sesion_responde_401(string metodo, string ruta)
    {
        var cuerpo = metodo == "POST" ? new { idMedicamento = 1, cantidad = "1" } : null;

        var respuesta = await EnviarAsync(api.CrearCliente(), new HttpMethod(metodo), ruta, cuerpo, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task La_busqueda_devuelve_el_medicamento_con_sus_lotes_vigentes_en_orden_FEFO()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, "30");
        await AgregarLoteAsync(cookie, id, "L-CORTO", 4, "20");

        var respuesta = await EnviarAsync(api.CrearCliente(), HttpMethod.Get,
            $"/api/salidas-medicamento/medicamentos?termino={Uri.EscapeDataString(nombre)}", null, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        var fila = Assert.Single(lista);
        Assert.Equal(50, fila.GetProperty("existenciasVigentes").GetInt32());
        Assert.False(fila.GetProperty("estaAgotado").GetBoolean());
        var lotes = fila.GetProperty("lotes").EnumerateArray().Select(l => l.GetProperty("numeroLote").GetString()).ToArray();
        Assert.Equal(new[] { "L-CORTO", "L-LARGO" }, lotes);
    }

    [Fact]
    public async Task La_propuesta_muestra_el_reparto_FEFO_y_no_cambia_el_stock()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, "30");
        await AgregarLoteAsync(cookie, id, "L-CORTO", 4, "20");

        var respuesta = await EnviarAsync(api.CrearCliente(), HttpMethod.Get,
            $"/api/salidas-medicamento/propuesta?idMedicamento={id}&cantidad=25", null, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var lotes = json.GetProperty("lotes").EnumerateArray().ToArray();
        Assert.Equal("L-CORTO", lotes[0].GetProperty("numeroLote").GetString());
        Assert.Equal(20, lotes[0].GetProperty("cantidad").GetInt32());
        Assert.Equal("L-LARGO", lotes[1].GetProperty("numeroLote").GetString());
        Assert.Equal(5, lotes[1].GetProperty("cantidad").GetInt32());
        Assert.Equal("20", await StockDeAsync(id, "L-CORTO"));
        Assert.Equal("30", await StockDeAsync(id, "L-LARGO"));
        Assert.Equal("0", await SalidasDeAsync(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-2")]
    [InlineData("1.5")]
    [InlineData("abc")]
    public async Task Cantidad_invalida_responde_400_con_el_mensaje_y_no_registra(string cantidad)
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, "10");

        var respuesta = await RegistrarAsync(cookie, id, cantidad);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var errores = json.GetProperty("errores").EnumerateArray().Select(e => e.GetString()).ToArray();
        var esperado = cantidad == string.Empty
            ? "Este campo es obligatorio"
            : "La cantidad debe ser un número entero mayor que 0";
        Assert.Contains(esperado, errores);
        Assert.Equal("10", await StockDeAsync(id, "L-LARGO"));
        Assert.Equal("0", await SalidasDeAsync(id));
    }

    [Fact]
    public async Task Registrar_salida_descuenta_lotes_FEFO_y_guarda_movimiento_con_detalles()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, "30");
        await AgregarLoteAsync(cookie, id, "L-CORTO", 4, "20");

        var respuesta = await RegistrarAsync(cookie, id, "25");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Salida registrada", json.GetProperty("mensaje").GetString());
        Assert.Equal(25, json.GetProperty("cantidadTotal").GetInt32());
        Assert.Equal("0", await StockDeAsync(id, "L-CORTO"));
        Assert.Equal("25", await StockDeAsync(id, "L-LARGO"));
        Assert.Equal("1", await SalidasDeAsync(id));
        Assert.Equal("2", await ConsultarAsync(
            "SELECT COUNT(*) FROM detalle_movimiento d JOIN movimiento m ON m.id_movimiento = d.id_movimiento " +
            "WHERE m.tipo = 'SALIDA' AND m.id_medicamento = @id;", id));
        Assert.Equal("25", await ConsultarAsync(
            "SELECT cantidad_total FROM movimiento WHERE tipo = 'SALIDA' AND id_medicamento = @id;", id));
    }

    [Fact]
    public async Task Stock_insuficiente_responde_422_y_no_cambia_nada()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, "10");

        var respuesta = await RegistrarAsync(cookie, id, "11");

        Assert.Equal((HttpStatusCode)422, respuesta.StatusCode);
        Assert.Contains("Stock insuficiente", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("10", await StockDeAsync(id, "L-LARGO"));
        Assert.Equal("0", await SalidasDeAsync(id));
    }

    [Fact]
    public async Task Lotes_vencidos_no_se_usan_y_sin_vigentes_responde_sin_existencias()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, "10");
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
            await using var comando = await conexion.CrearComandoAsync(
                "UPDATE lote SET fecha_caducidad = DATE_SUB(CURDATE(), INTERVAL 2 DAY) WHERE id_medicamento = @id;",
                CancellationToken.None);
            comando.Parameters.AddWithValue("@id", id);
            await comando.ExecuteNonQueryAsync();
            await conexion.GuardarCambiosAsync(CancellationToken.None);
        }

        var respuesta = await RegistrarAsync(cookie, id, "1");

        Assert.Equal((HttpStatusCode)422, respuesta.StatusCode);
        Assert.Contains("Sin existencias", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("10", await StockDeAsync(id, "L-LARGO"));
    }

    [Fact]
    public async Task Medicamento_inexistente_responde_404()
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await RegistrarAsync(cookie, 999999, "1");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Si_falla_el_registro_del_movimiento_se_revierte_el_descuento_de_stock()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, "30");

        var respuesta = await RegistrarAsync(cookie, id, "10", apiFalla.CrearCliente());

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("30", await StockDeAsync(id, "L-LARGO"));
        Assert.Equal("0", await SalidasDeAsync(id));
    }

    [Fact]
    public async Task Dos_salidas_simultaneas_que_juntas_superan_el_stock_no_dejan_stock_negativo()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, "10");

        var respuestas = await Task.WhenAll(
            RegistrarAsync(cookie, id, "7"),
            RegistrarAsync(cookie, id, "7"));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, respuestas.Count(r => r.StatusCode == (HttpStatusCode)422));
        Assert.Equal("3", await StockDeAsync(id, "L-LARGO"));
        Assert.Equal("1", await SalidasDeAsync(id));
    }
}
