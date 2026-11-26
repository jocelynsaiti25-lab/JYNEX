using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.SalidaMedicamentos;

[Trait("Category", "Integracion")]
public class SalidaMedicamentosEndpointsTests(ApiConMySql api, ApiConSalidaQueFalla apiSalidaFalla)
    : IClassFixture<ApiConMySql>, IClassFixture<ApiConSalidaQueFalla>
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

    private Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, object? cuerpo, string? cookie) =>
        EnviarAsync(api.CrearCliente(), metodo, ruta, cuerpo, cookie);

    private async Task<int> CrearMedicamentoAsync(string cookie)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/medicamentos", new
        {
            nombreComercial = $"Salida {Unico()}",
            nombreSustancia = "Paracetamol",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-INICIAL",
            fechaCaducidad = Fecha(30),
            cantidadInicial = "0"
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("idMedicamento").GetInt32();
    }

    private async Task<int> AgregarLoteAsync(string cookie, int idMedicamento, string numeroLote, int meses, int cantidad)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/entradas-lote",
            new { idMedicamento, numeroLote, fechaCaducidad = Fecha(meses), cantidad = cantidad.ToString() }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("idLote").GetInt32();
    }

    private Task<HttpResponseMessage> RegistrarSalidaAsync(string? cookie, int idMedicamento, int cantidad) =>
        EnviarAsync(HttpMethod.Post, "/api/salidas-medicamentos", new { idMedicamento, cantidad }, cookie);

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

    private async Task<long> LeerAsync(string sql, int id)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", id);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    private Task<long> StockAsync(int idLote) => LeerAsync("SELECT stock FROM lote WHERE id_lote = @id;", idLote);

    private Task<long> SalidasAsync(int idMedicamento) =>
        LeerAsync("SELECT COUNT(*) FROM movimiento WHERE id_medicamento = @id AND tipo = 'SALIDA';", idMedicamento);

    private Task<long> SalidaDelLoteAsync(int idLote) =>
        LeerAsync("""
            SELECT COALESCE(SUM(d.cantidad), 0)
            FROM movimiento m
            INNER JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento
            WHERE d.id_lote = @id AND m.tipo = 'SALIDA';
            """, idLote);

    [Fact]
    public async Task Registra_la_salida_en_orden_FEFO_descuenta_en_MySQL_y_guarda_movimiento_y_detalles()
    {
        var cookie = await IniciarSesionAsync();
        var id = await CrearMedicamentoAsync(cookie);
        var tercero = await AgregarLoteAsync(cookie, id, "L003", 14, 20);
        var primero = await AgregarLoteAsync(cookie, id, "L001", 4, 5);
        var segundo = await AgregarLoteAsync(cookie, id, "L002", 6, 10);

        var respuesta = await RegistrarSalidaAsync(cookie, id, 12);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(0, await StockAsync(primero));
        Assert.Equal(3, await StockAsync(segundo));
        Assert.Equal(20, await StockAsync(tercero));
        Assert.Equal(1, await SalidasAsync(id));
        Assert.Equal(5, await SalidaDelLoteAsync(primero));
        Assert.Equal(7, await SalidaDelLoteAsync(segundo));
        Assert.Equal(0, await SalidaDelLoteAsync(tercero));
        Assert.Equal(12, await LeerAsync(
            "SELECT cantidad_total FROM movimiento WHERE id_medicamento = @id AND tipo = 'SALIDA';", id));
    }

    [Fact]
    public async Task La_propuesta_muestra_los_lotes_FEFO_sin_cambiar_el_inventario()
    {
        var cookie = await IniciarSesionAsync();
        var id = await CrearMedicamentoAsync(cookie);
        var primero = await AgregarLoteAsync(cookie, id, "L001", 4, 5);
        var segundo = await AgregarLoteAsync(cookie, id, "L002", 6, 10);

        var respuesta = await EnviarAsync(HttpMethod.Get,
            $"/api/salidas-medicamentos/propuesta?idMedicamento={id}&cantidad=8", cuerpo: null, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("puedeRealizarse").GetBoolean());
        var lotes = json.GetProperty("lotes").EnumerateArray()
            .Select(l => (l.GetProperty("idLote").GetInt32(), l.GetProperty("cantidadAUtilizar").GetInt32()))
            .ToList();
        Assert.Equal(new[] { (primero, 5), (segundo, 3) }, lotes);
        Assert.Equal(5, await StockAsync(primero));
        Assert.Equal(0, await SalidasAsync(id));
    }

    [Fact]
    public async Task Con_existencias_insuficientes_responde_422_y_no_cambia_nada()
    {
        var cookie = await IniciarSesionAsync();
        var id = await CrearMedicamentoAsync(cookie);
        var lote = await AgregarLoteAsync(cookie, id, "L001", 6, 5);

        var respuesta = await RegistrarSalidaAsync(cookie, id, 6);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal(5, await StockAsync(lote));
        Assert.Equal(0, await SalidasAsync(id));
    }

    [Fact]
    public async Task Los_lotes_caducados_no_se_usan_aunque_tengan_existencias()
    {
        var cookie = await IniciarSesionAsync();
        var id = await CrearMedicamentoAsync(cookie);
        var caducado = await AgregarLoteAsync(cookie, id, "L-CAD", 6, 50);
        var vigente = await AgregarLoteAsync(cookie, id, "L-VIG", 8, 10);
        await EjecutarAsync("UPDATE lote SET fecha_caducidad = '2000-01-01' WHERE id_lote = @id;", caducado);

        var insuficiente = await RegistrarSalidaAsync(cookie, id, 11);
        var correcta = await RegistrarSalidaAsync(cookie, id, 10);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, insuficiente.StatusCode);
        Assert.Equal(HttpStatusCode.Created, correcta.StatusCode);
        Assert.Equal(50, await StockAsync(caducado));
        Assert.Equal(0, await StockAsync(vigente));
    }

    [Fact]
    public async Task Dos_salidas_al_mismo_tiempo_nunca_dejan_stock_negativo()
    {
        var cookie = await IniciarSesionAsync();
        var id = await CrearMedicamentoAsync(cookie);
        var lote = await AgregarLoteAsync(cookie, id, "L-CARRERA", 6, 40);

        var respuestas = await Task.WhenAll(
            RegistrarSalidaAsync(cookie, id, 30),
            RegistrarSalidaAsync(cookie, id, 30));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal(10, await StockAsync(lote));
        Assert.Equal(1, await SalidasAsync(id));
    }

    [Fact]
    public async Task Si_falla_el_registro_del_movimiento_el_stock_no_queda_descontado()
    {
        var cookie = await IniciarSesionAsync();
        var id = await CrearMedicamentoAsync(cookie);
        var lote = await AgregarLoteAsync(cookie, id, "L-FALLA", 6, 10);

        var respuesta = await EnviarAsync(apiSalidaFalla.CrearCliente(), HttpMethod.Post,
            "/api/salidas-medicamentos", new { idMedicamento = id, cantidad = 4 }, cookie);

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal(10, await StockAsync(lote));
        Assert.Equal(0, await SalidasAsync(id));
    }

    [Fact]
    public async Task Un_medicamento_inactivo_o_inexistente_responde_404()
    {
        var cookie = await IniciarSesionAsync();
        var id = await CrearMedicamentoAsync(cookie);
        await AgregarLoteAsync(cookie, id, "L001", 6, 10);
        await EjecutarAsync("UPDATE medicamento SET activo = 0 WHERE id_medicamento = @id;", id);

        Assert.Equal(HttpStatusCode.NotFound, (await RegistrarSalidaAsync(cookie, id, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await RegistrarSalidaAsync(cookie, 99999999, 1)).StatusCode);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 0)]
    [InlineData(1, -3)]
    public async Task Datos_invalidos_responden_400_en_la_salida_y_en_la_propuesta(int idMedicamento, int cantidad)
    {
        var cookie = await IniciarSesionAsync();

        var salida = await RegistrarSalidaAsync(cookie, idMedicamento, cantidad);
        var propuesta = await EnviarAsync(HttpMethod.Get,
            $"/api/salidas-medicamentos/propuesta?idMedicamento={idMedicamento}&cantidad={cantidad}", cuerpo: null, cookie);

        Assert.Equal(HttpStatusCode.BadRequest, salida.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, propuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var salida = await RegistrarSalidaAsync(cookie: null, 1, 1);
        var propuesta = await EnviarAsync(HttpMethod.Get,
            "/api/salidas-medicamentos/propuesta?idMedicamento=1&cantidad=1", cuerpo: null, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, salida.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, propuesta.StatusCode);
    }
}
