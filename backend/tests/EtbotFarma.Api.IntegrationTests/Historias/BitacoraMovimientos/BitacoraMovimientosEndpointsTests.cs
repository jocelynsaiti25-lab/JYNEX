using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.BitacoraMovimientos;

[Trait("Category", "Integracion")]
public class BitacoraMovimientosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private static string Fecha(int meses) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(meses).ToString("yyyy-MM-dd");

    private static string Dia(int desplazamiento) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(desplazamiento).ToString("yyyy-MM-dd");

    private async Task<string> IniciarSesionAsync(string nombreUsuario, string contrasenia)
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar",
            new { nombreUsuario, contrasenia });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private Task<string> IniciarSesionAdminAsync() => IniciarSesionAsync("admin", ContraseniaAdmin);

    private async Task<(string Cookie, int IdUsuario)> CrearEmpleadoYEntrarAsync(string cookieAdmin)
    {
        var sufijo = Unico();
        var nombreUsuario = $"bitacora.{sufijo}";
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", new
        {
            nombre = "Luis",
            apellidos = "Prieto",
            nombreUsuario,
            correo = $"bitacora.{sufijo}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia = "Farmacia#2026",
            idRol = 2
        }, cookieAdmin);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var cookie = await IniciarSesionAsync(nombreUsuario, "Farmacia#2026");
        return (cookie, json.GetProperty("idUsuario").GetInt32());
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

    private async Task<int> CrearMedicamentoAsync(string cookie, int cantidadInicial)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/medicamentos", new
        {
            nombreComercial = $"Bitacora {Unico()}",
            nombreSustancia = "Sustancia de prueba",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-A",
            fechaCaducidad = Fecha(12),
            cantidadInicial = cantidadInicial.ToString()
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("idMedicamento").GetInt32();
    }

    private async Task<int> AgregarLoteAsync(string cookie, int idMedicamento, string numeroLote, int cantidad)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/entradas-lote", new
        {
            idMedicamento,
            numeroLote,
            fechaCaducidad = Fecha(24),
            cantidad = cantidad.ToString()
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("idLote").GetInt32();
    }

    private async Task RegistrarSalidaAsync(string cookie, int idMedicamento, int cantidad)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/salidas-medicamento",
            new { idMedicamento, cantidad = cantidad.ToString() }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    private async Task DarDeBajaAsync(string cookie, int idLote, int cantidad, string motivo)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, $"/api/inventario/lotes/{idLote}/baja",
            new { cantidadConsultada = cantidad, motivo }, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    private async Task<JsonElement> ConsultarAsync(string cookie, string consulta)
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/bitacora/movimientos?{consulta}", null, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<long> ContarMovimientosAsync(int idMedicamento)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(
            "SELECT COUNT(*) FROM movimiento WHERE id_medicamento = @id;", CancellationToken.None);
        comando.Parameters.AddWithValue("@id", idMedicamento);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    private async Task CaducarLoteAsync(int idLote)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using (var comando = await conexion.CrearComandoAsync(
            "UPDATE lote SET fecha_caducidad = '2000-01-01' WHERE id_lote = @id;", CancellationToken.None))
        {
            comando.Parameters.AddWithValue("@id", idLote);
            await comando.ExecuteNonQueryAsync();
        }
        await conexion.GuardarCambiosAsync(CancellationToken.None);
    }

    private async Task<int> PrepararMedicamentoConHistorialAsync(string cookie)
    {
        var idMedicamento = await CrearMedicamentoAsync(cookie, 10);
        var idLoteB = await AgregarLoteAsync(cookie, idMedicamento, "L-B", 10);
        await RegistrarSalidaAsync(cookie, idMedicamento, 15);
        await CaducarLoteAsync(idLoteB);
        await DarDeBajaAsync(cookie, idLoteB, 5, "Caducado");
        return idMedicamento;
    }

    private static string[] Tipos(JsonElement pagina) =>
        pagina.GetProperty("movimientos").EnumerateArray().Select(m => m.GetProperty("tipo").GetString()!).ToArray();

    [Theory]
    [InlineData("/api/bitacora/movimientos")]
    [InlineData("/api/bitacora/medicamentos")]
    public async Task Sin_sesion_responde_401(string ruta)
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, ruta, null, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/api/bitacora/movimientos")]
    [InlineData("/api/bitacora/medicamentos")]
    public async Task El_empleado_no_puede_consultar_y_recibe_403(string ruta)
    {
        var admin = await IniciarSesionAdminAsync();
        var (cookieEmpleado, _) = await CrearEmpleadoYEntrarAsync(admin);

        var respuesta = await EnviarAsync(HttpMethod.Get, ruta, null, cookieEmpleado);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_administrador_ve_entradas_salida_con_varios_lotes_y_baja_del_mas_reciente_al_mas_antiguo()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);

        var pagina = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}");

        Assert.Equal(new[] { "BAJA", "SALIDA", "ENTRADA", "ENTRADA" }, Tipos(pagina));
        Assert.Equal(4, pagina.GetProperty("total").GetInt32());
        var movimientos = pagina.GetProperty("movimientos").EnumerateArray().ToList();

        var baja = movimientos[0];
        Assert.Equal(5, baja.GetProperty("cantidadTotal").GetInt32());
        Assert.Equal("Caducado", baja.GetProperty("motivo").GetString());
        Assert.Equal("L-B", baja.GetProperty("lotes")[0].GetProperty("numeroLote").GetString());

        var salida = movimientos[1];
        Assert.Equal(15, salida.GetProperty("cantidadTotal").GetInt32());
        Assert.Equal(JsonValueKind.Null, salida.GetProperty("motivo").ValueKind);
        var lotesSalida = salida.GetProperty("lotes").EnumerateArray().ToList();
        Assert.Equal(2, lotesSalida.Count);
        Assert.Equal(15, lotesSalida.Sum(l => l.GetProperty("cantidad").GetInt32()));

        foreach (var entrada in movimientos.Skip(2))
        {
            Assert.Equal(JsonValueKind.Null, entrada.GetProperty("motivo").ValueKind);
            Assert.Equal(10, entrada.GetProperty("cantidadTotal").GetInt32());
            Assert.Single(entrada.GetProperty("lotes").EnumerateArray());
        }

        foreach (var movimiento in movimientos)
        {
            Assert.False(string.IsNullOrWhiteSpace(movimiento.GetProperty("usuario").GetString()));
            Assert.True(movimiento.GetProperty("fecha").GetDateTimeOffset().UtcDateTime > DateTime.UtcNow.AddMinutes(-10));
            Assert.Equal(idMedicamento, movimiento.GetProperty("idMedicamento").GetInt32());
        }
    }

    [Fact]
    public async Task Filtra_por_tipo_de_movimiento()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);

        Assert.Equal(new[] { "SALIDA" }, Tipos(await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&tipo=SALIDA")));
        Assert.Equal(new[] { "BAJA" }, Tipos(await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&tipo=baja")));
        Assert.Equal(2, (await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&tipo=ENTRADA")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Filtra_por_usuario_y_conserva_el_historial_de_cada_persona()
    {
        var admin = await IniciarSesionAdminAsync();
        var (cookieEmpleado, idEmpleado) = await CrearEmpleadoYEntrarAsync(admin);
        var idMedicamento = await CrearMedicamentoAsync(admin, 10);
        await AgregarLoteAsync(cookieEmpleado, idMedicamento, "L-EMP", 4);

        var delEmpleado = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&idUsuario={idEmpleado}");

        Assert.Equal(1, delEmpleado.GetProperty("total").GetInt32());
        var movimiento = delEmpleado.GetProperty("movimientos")[0];
        Assert.Equal(idEmpleado, movimiento.GetProperty("idUsuario").GetInt32());
        Assert.Equal("Luis Prieto", movimiento.GetProperty("usuario").GetString());
        Assert.Equal(2, (await ConsultarAsync(admin, $"idMedicamento={idMedicamento}")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Filtra_por_rango_de_fechas_incluyendo_el_dia_inicial_y_el_final()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);

        var dentro = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&desde={Dia(-1)}&hasta={Dia(1)}");
        var futuro = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&desde={Dia(5)}&hasta={Dia(9)}");
        var pasado = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&desde={Dia(-9)}&hasta={Dia(-5)}");

        Assert.Equal(4, dentro.GetProperty("total").GetInt32());
        Assert.Equal(0, futuro.GetProperty("total").GetInt32());
        Assert.Equal(0, pasado.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Combina_todos_los_filtros_a_la_vez()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);
        var todos = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}");
        var idUsuario = todos.GetProperty("movimientos")[0].GetProperty("idUsuario").GetInt32();

        var combinado = await ConsultarAsync(admin,
            $"desde={Dia(-1)}&hasta={Dia(1)}&idUsuario={idUsuario}&idMedicamento={idMedicamento}&tipo=BAJA");
        var sinCoincidencia = await ConsultarAsync(admin,
            $"desde={Dia(5)}&hasta={Dia(6)}&idUsuario={idUsuario}&idMedicamento={idMedicamento}&tipo=BAJA");

        Assert.Equal(new[] { "BAJA" }, Tipos(combinado));
        Assert.Equal(0, sinCoincidencia.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Sin_coincidencias_responde_200_con_lista_vacia()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin, 10);

        var pagina = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&tipo=BAJA");

        Assert.Empty(pagina.GetProperty("movimientos").EnumerateArray());
        Assert.Equal(0, pagina.GetProperty("total").GetInt32());
        Assert.Equal(0, pagina.GetProperty("totalPaginas").GetInt32());
    }

    [Fact]
    public async Task La_paginacion_conserva_el_orden_y_los_filtros()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);

        var primera = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&pagina=1&tamanoPagina=3");
        var segunda = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&pagina=2&tamanoPagina=3");
        var fuera = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&pagina=3&tamanoPagina=3");

        Assert.Equal(new[] { "BAJA", "SALIDA", "ENTRADA" }, Tipos(primera));
        Assert.Equal(new[] { "ENTRADA" }, Tipos(segunda));
        Assert.Equal(4, primera.GetProperty("total").GetInt32());
        Assert.Equal(2, primera.GetProperty("totalPaginas").GetInt32());
        Assert.Empty(fuera.GetProperty("movimientos").EnumerateArray());

        var soloSalidas = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&tipo=SALIDA&pagina=1&tamanoPagina=1");
        Assert.Equal(new[] { "SALIDA" }, Tipos(soloSalidas));
        Assert.Equal(1, soloSalidas.GetProperty("totalPaginas").GetInt32());
    }

    [Fact]
    public async Task Un_movimiento_nuevo_aparece_al_inicio_de_la_lista()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin, 10);
        var antes = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}");

        await RegistrarSalidaAsync(admin, idMedicamento, 3);
        var despues = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}");

        Assert.Equal(new[] { "ENTRADA" }, Tipos(antes));
        Assert.Equal(new[] { "SALIDA", "ENTRADA" }, Tipos(despues));
    }

    [Theory]
    [InlineData("desde=2026-05-11&hasta=2026-05-10")]
    [InlineData("desde=1999-01-01")]
    [InlineData("tipo=TRASLADO")]
    [InlineData("idUsuario=0")]
    [InlineData("idMedicamento=-1")]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=0")]
    [InlineData("tamanoPagina=101")]
    public async Task Parametros_no_validos_responden_400_con_mensajes(string consulta)
    {
        var admin = await IniciarSesionAdminAsync();

        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/bitacora/movimientos?{consulta}", null, admin);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(json.GetProperty("errores").EnumerateArray());
    }

    [Theory]
    [InlineData("desde=no-es-fecha")]
    [InlineData("hasta=2026-13-45")]
    public async Task Una_fecha_que_no_existe_responde_400(string consulta)
    {
        var admin = await IniciarSesionAdminAsync();

        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/bitacora/movimientos?{consulta}", null, admin);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task La_bitacora_es_de_solo_lectura_y_rechaza_cualquier_cambio(string metodo)
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);
        var movimientosAntes = await ContarMovimientosAsync(idMedicamento);
        var primero = (await ConsultarAsync(admin, $"idMedicamento={idMedicamento}"))
            .GetProperty("movimientos")[0].GetProperty("idMovimiento").GetInt32();

        var contraLaLista = await EnviarAsync(new HttpMethod(metodo), "/api/bitacora/movimientos", new { }, admin);
        var contraUnMovimiento = await EnviarAsync(new HttpMethod(metodo), $"/api/bitacora/movimientos/{primero}", new { }, admin);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, contraLaLista.StatusCode);
        Assert.True(contraUnMovimiento.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
        Assert.Equal(movimientosAntes, await ContarMovimientosAsync(idMedicamento));
    }

    [Fact]
    public async Task Consultar_no_genera_movimientos_nuevos()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);
        var antes = await ContarMovimientosAsync(idMedicamento);

        await ConsultarAsync(admin, $"idMedicamento={idMedicamento}");
        await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&tipo=BAJA");

        Assert.Equal(antes, await ContarMovimientosAsync(idMedicamento));
    }

    [Fact]
    public async Task El_historial_se_conserva_aunque_el_lote_quede_en_cero_y_el_usuario_este_inactivo()
    {
        var admin = await IniciarSesionAdminAsync();
        var (cookieEmpleado, idEmpleado) = await CrearEmpleadoYEntrarAsync(admin);
        var idMedicamento = await CrearMedicamentoAsync(admin, 10);
        var idLote = await AgregarLoteAsync(cookieEmpleado, idMedicamento, "L-CERO", 6);
        await CaducarLoteAsync(idLote);
        await DarDeBajaAsync(admin, idLote, 6, "Retiro total");

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
            await using var comando = await conexion.CrearComandoAsync(
                "UPDATE usuario SET activo = FALSE WHERE id_usuario = @id;", CancellationToken.None);
            comando.Parameters.AddWithValue("@id", idEmpleado);
            await comando.ExecuteNonQueryAsync();
            await conexion.GuardarCambiosAsync(CancellationToken.None);
        }

        var pagina = await ConsultarAsync(admin, $"idMedicamento={idMedicamento}&idUsuario={idEmpleado}");

        var movimiento = pagina.GetProperty("movimientos")[0];
        Assert.Equal("Luis Prieto", movimiento.GetProperty("usuario").GetString());
        Assert.Equal("L-CERO", movimiento.GetProperty("lotes")[0].GetProperty("numeroLote").GetString());
    }

    [Fact]
    public async Task La_lista_de_medicamentos_incluye_los_que_tienen_movimientos()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin, 10);

        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/bitacora/medicamentos", null, admin);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(lista.EnumerateArray(), m => m.GetProperty("idMedicamento").GetInt32() == idMedicamento);
    }
}
