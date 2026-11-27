using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.BajaLotes;

[Trait("Category", "Integracion")]
public class BajaLotesEndpointsTests(ApiConMySql api, ApiConBitacoraQueFalla apiBitacoraFalla)
    : IClassFixture<ApiConMySql>, IClassFixture<ApiConBitacoraQueFalla>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private static string Fecha(int meses) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(meses).ToString("yyyy-MM-dd");

    private async Task<string> IniciarSesionAsync(string nombreUsuario = "admin", string? contrasenia = null)
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar",
            new { nombreUsuario, contrasenia = contrasenia ?? ContraseniaAdmin });
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
            nombreComercial = $"Baja {Unico()}",
            nombreSustancia = "Paracetamol",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-INICIAL",
            fechaCaducidad = Fecha(24),
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

    private Task<HttpResponseMessage> DarDeBajaAsync(string? cookie, int idLote, int cantidadConsultada, string? motivo) =>
        EnviarAsync(HttpMethod.Post, $"/api/inventario/lotes/{idLote}/baja", new { cantidadConsultada, motivo }, cookie);

    private async Task<long> LeerAsync(string sql, int id)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", id);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    private Task<long> StockAsync(int idLote) =>
        LeerAsync("SELECT stock FROM lote WHERE id_lote = @id;", idLote);

    private Task<long> BajasAsync(int idLote) =>
        LeerAsync("""
            SELECT COUNT(*)
            FROM movimiento m
            INNER JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento
            WHERE d.id_lote = @id AND m.tipo = 'BAJA';
            """, idLote);

    private async Task FijarCaducidadAsync(int idLote, string fecha)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using (var comando = await conexion.CrearComandoAsync(
            "UPDATE lote SET fecha_caducidad = @fecha WHERE id_lote = @id;", CancellationToken.None))
        {
            comando.Parameters.AddWithValue("@fecha", fecha);
            comando.Parameters.AddWithValue("@id", idLote);
            await comando.ExecuteNonQueryAsync();
        }
        await conexion.GuardarCambiosAsync(CancellationToken.None);
    }

    private async Task<string> CrearEmpleadoAsync(string admin)
    {
        var nombreUsuario = $"baja.{Unico()}";
        const string contrasenia = "Farmacia#2026";
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", new
        {
            nombre = "Luis",
            apellidos = "Pérez",
            nombreUsuario,
            correo = $"{nombreUsuario}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia,
            idRol = 2
        }, admin);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await IniciarSesionAsync(nombreUsuario, contrasenia);
    }

    [Fact]
    public async Task Da_de_baja_un_lote_rojo_deja_el_stock_en_cero_y_registra_la_baja_en_la_bitacora()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-2301", 2, 40);

        var respuesta = await DarDeBajaAsync(admin, idLote, 40, "Próximo a caducar");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Lote dado de baja", json.GetProperty("mensaje").GetString());
        Assert.Equal(40, json.GetProperty("cantidadRetirada").GetInt32());
        Assert.Equal("L-2301", json.GetProperty("numeroLote").GetString());

        Assert.Equal(0, await StockAsync(idLote));
        Assert.Equal(1, await LeerAsync("""
            SELECT COUNT(*)
            FROM movimiento m
            INNER JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento
            WHERE d.id_lote = @id
              AND m.tipo = 'BAJA'
              AND m.cantidad_total = 40
              AND d.cantidad = 40
              AND m.motivo = 'Próximo a caducar'
              AND m.id_usuario IS NOT NULL;
            """, idLote));
        Assert.Equal(1, await LeerAsync("SELECT COUNT(*) FROM lote WHERE id_lote = @id;", idLote));
    }

    [Fact]
    public async Task Un_lote_ya_caducado_tambien_se_da_de_baja()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-VIEJO", 2, 15);
        await FijarCaducidadAsync(idLote, "2000-01-01");

        var respuesta = await DarDeBajaAsync(admin, idLote, 15, "Caducado");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(0, await StockAsync(idLote));
        Assert.Equal(1, await BajasAsync(idLote));
    }

    [Fact]
    public async Task Un_empleado_tambien_puede_dar_de_baja()
    {
        var admin = await IniciarSesionAsync();
        var empleado = await CrearEmpleadoAsync(admin);
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-EMP", 1, 8);

        var respuesta = await DarDeBajaAsync(empleado, idLote, 8, "Próximo a caducar");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(0, await StockAsync(idLote));
    }

    [Fact]
    public async Task Sin_sesion_responde_401_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-ANON", 1, 10);

        var respuesta = await DarDeBajaAsync(cookie: null, idLote, 10, "Caducado");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(10, await StockAsync(idLote));
        Assert.Equal(0, await BajasAsync(idLote));
    }

    private async Task<int> PrepararLoteParaMotivoAsync(string admin, string numeroLote)
    {
        var idMedicamento = await CrearMedicamentoAsync(admin);
        return await AgregarLoteAsync(admin, idMedicamento, numeroLote, 1, 10);
    }

    private async Task AfirmarMotivoRechazadoAsync(HttpResponseMessage respuesta, int idLote, string mensaje)
    {
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var errores = json.GetProperty("errores").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains(mensaje, errores);
        Assert.Equal(10, await StockAsync(idLote));
        Assert.Equal(0, await BajasAsync(idLote));
    }

    [Fact]
    public async Task Un_motivo_null_responde_400_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync();
        var idLote = await PrepararLoteParaMotivoAsync(admin, "L-MOT-NULL");

        var respuesta = await DarDeBajaAsync(admin, idLote, 10, null);

        await AfirmarMotivoRechazadoAsync(respuesta, idLote, "El motivo es obligatorio.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("     ")]
    public async Task Un_motivo_vacio_o_solo_con_espacios_responde_400_y_no_cambia_nada(string motivo)
    {
        var admin = await IniciarSesionAsync();
        var idLote = await PrepararLoteParaMotivoAsync(admin, "L-MOT-VACIO");

        var respuesta = await DarDeBajaAsync(admin, idLote, 10, motivo);

        await AfirmarMotivoRechazadoAsync(respuesta, idLote, "El motivo es obligatorio.");
    }

    [Fact]
    public async Task Un_motivo_de_mas_de_200_caracteres_responde_400_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync();
        var idLote = await PrepararLoteParaMotivoAsync(admin, "L-MOT-LARGO");

        var respuesta = await DarDeBajaAsync(admin, idLote, 10, new string('m', 201));

        await AfirmarMotivoRechazadoAsync(respuesta, idLote, "El motivo no puede tener más de 200 caracteres.");
    }

    [Fact]
    public async Task Un_motivo_valido_de_exactamente_200_caracteres_se_acepta()
    {
        var admin = await IniciarSesionAsync();
        var idLote = await PrepararLoteParaMotivoAsync(admin, "L-MOT-200");

        var respuesta = await DarDeBajaAsync(admin, idLote, 10, new string('m', 200));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(0, await StockAsync(idLote));
        Assert.Equal(1, await BajasAsync(idLote));
    }

    [Fact]
    public async Task Si_las_existencias_cambiaron_desde_la_consulta_responde_que_el_lote_ya_no_es_elegible()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-CAMBIO", 1, 10);

        var respuesta = await DarDeBajaAsync(admin, idLote, 12, "Caducado");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("El lote ya no es elegible", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(10, await StockAsync(idLote));
    }

    [Fact]
    public async Task Un_lote_ya_dado_de_baja_no_se_puede_dar_de_baja_otra_vez()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-DOBLE", 1, 10);

        Assert.Equal(HttpStatusCode.OK, (await DarDeBajaAsync(admin, idLote, 10, "Caducado")).StatusCode);
        var segunda = await DarDeBajaAsync(admin, idLote, 10, "Caducado");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, segunda.StatusCode);
        Assert.Equal(1, await BajasAsync(idLote));
    }

    [Fact]
    public async Task Dos_bajas_al_mismo_tiempo_solo_aplican_una()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-CARRERA", 1, 25);

        var respuestas = await Task.WhenAll(
            DarDeBajaAsync(admin, idLote, 25, "Caducado"),
            DarDeBajaAsync(admin, idLote, 25, "Caducado"));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal(0, await StockAsync(idLote));
        Assert.Equal(1, await BajasAsync(idLote));
    }

    [Fact]
    public async Task Un_lote_inexistente_responde_404()
    {
        var admin = await IniciarSesionAsync();

        var respuesta = await DarDeBajaAsync(admin, 99999999, 10, "Caducado");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(24)]
    public async Task Un_lote_amarillo_o_verde_responde_que_ya_no_es_elegible_y_no_cambia_nada(int meses)
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-NOROJO", meses, 10);

        var respuesta = await DarDeBajaAsync(admin, idLote, 10, "Caducado");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("El lote ya no es elegible", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(10, await StockAsync(idLote));
        Assert.Equal(0, await BajasAsync(idLote));
    }

    [Fact]
    public async Task Si_falla_la_bitacora_no_queda_ningun_cambio_a_medias()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-FALLA", 1, 10);

        var respuesta = await EnviarAsync(apiBitacoraFalla.CrearCliente(), HttpMethod.Post,
            $"/api/inventario/lotes/{idLote}/baja", new { cantidadConsultada = 10, motivo = "Caducado" }, admin);

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal(10, await StockAsync(idLote));
        Assert.Equal(0, await BajasAsync(idLote));
    }

    [Fact]
    public async Task Solo_se_retira_el_lote_rojo_y_el_medicamento_sigue_disponible_con_sus_otros_lotes()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idRojo = await AgregarLoteAsync(admin, idMedicamento, "L-ROJO", 1, 10);
        var idVerde = await AgregarLoteAsync(admin, idMedicamento, "L-VERDE", 24, 30);

        Assert.Equal(HttpStatusCode.OK, (await DarDeBajaAsync(admin, idRojo, 10, "Próximo a caducar")).StatusCode);

        Assert.Equal(0, await StockAsync(idRojo));
        Assert.Equal(30, await StockAsync(idVerde));
        var inventario = await LeerInventarioAsync(admin, idMedicamento);
        var fila = Assert.Single(inventario);
        Assert.Equal(idVerde, fila.GetProperty("idLote").GetInt32());
        Assert.Equal("Disponible", fila.GetProperty("estadoDisponibilidad").GetString());
    }

    [Fact]
    public async Task Si_era_el_unico_lote_con_existencias_el_medicamento_queda_agotado()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        var idLote = await AgregarLoteAsync(admin, idMedicamento, "L-UNICO", 1, 10);

        Assert.Equal(HttpStatusCode.OK, (await DarDeBajaAsync(admin, idLote, 10, "Próximo a caducar")).StatusCode);

        var fila = Assert.Single(await LeerInventarioAsync(admin, idMedicamento));
        Assert.Equal("Agotado", fila.GetProperty("estadoDisponibilidad").GetString());
        Assert.Equal(JsonValueKind.Null, fila.GetProperty("idLote").ValueKind);
    }

    private async Task<List<JsonElement>> LeerInventarioAsync(string cookie, int idMedicamento)
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/inventario", cuerpo: null, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.EnumerateArray().Where(f => f.GetProperty("idMedicamento").GetInt32() == idMedicamento).ToList();
    }
}
