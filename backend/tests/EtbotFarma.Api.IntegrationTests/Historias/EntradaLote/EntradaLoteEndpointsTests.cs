using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.EntradaLote;

[Trait("Category", "Integracion")]
public class EntradaLoteEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
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

    private async Task<(int IdMedicamento, string Nombre)> CrearMedicamentoAsync(string cookie, string cantidadInicial = "0")
    {
        var nombre = $"Entrada {Unico()}";
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

    private Task<HttpResponseMessage> RegistrarEntradaAsync(string cookie, int idMedicamento, string numeroLote, string fecha, string cantidad) =>
        EnviarAsync(HttpMethod.Post, "/api/entradas-lote", new { idMedicamento, numeroLote, fechaCaducidad = fecha, cantidad }, cookie);

    private async Task<long> ContarAsync(string sql, int idMedicamento)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", idMedicamento);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Sin_sesion_responde_401_al_registrar_y_al_buscar()
    {
        var registrar = await EnviarAsync(HttpMethod.Post, "/api/entradas-lote",
            new { idMedicamento = 1, numeroLote = "L-1", fechaCaducidad = Fecha(24), cantidad = "5" }, cookie: null);
        var buscar = await EnviarAsync(HttpMethod.Get, "/api/entradas-lote/medicamentos?termino=a", cuerpo: null, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, registrar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, buscar.StatusCode);
    }

    [Fact]
    public async Task Registra_el_lote_la_entrada_en_la_bitacora_y_devuelve_las_existencias()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, cantidadInicial: "10");

        var respuesta = await RegistrarEntradaAsync(cookie, id, "L-NUEVO", Fecha(24), "25");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(25, json.GetProperty("cantidadIngresada").GetInt32());
        Assert.Equal(35, json.GetProperty("existenciasTotalesActualizadas").GetInt32());
        Assert.Equal("Verde", json.GetProperty("semaforoLote").GetString());
        Assert.False(json.GetProperty("estaAgotado").GetBoolean());

        Assert.Equal(1, await ContarAsync("SELECT COUNT(*) FROM lote WHERE id_medicamento = @id AND numero_lote = 'L-NUEVO' AND stock = 25;", id));
        Assert.Equal(1, await ContarAsync(
            "SELECT COUNT(*) FROM movimiento m INNER JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento WHERE m.id_medicamento = @id AND m.tipo = 'ENTRADA' AND m.cantidad_total = 25 AND d.cantidad = 25;", id));
    }

    [Fact]
    public async Task El_semaforo_general_toma_el_lote_mas_cercano_a_caducar()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie, cantidadInicial: "10");

        var respuesta = await RegistrarEntradaAsync(cookie, id, "L-ROJO", Fecha(2), "5");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Rojo", json.GetProperty("semaforoLote").GetString());
        Assert.Equal("Rojo", json.GetProperty("semaforoGeneralActualizado").GetString());
    }

    [Fact]
    public async Task Un_numero_de_lote_repetido_en_el_mismo_medicamento_responde_422_pero_en_otro_se_permite()
    {
        var cookie = await IniciarSesionAsync();
        var (primero, _) = await CrearMedicamentoAsync(cookie);
        var (segundo, _) = await CrearMedicamentoAsync(cookie);
        Assert.Equal(HttpStatusCode.Created, (await RegistrarEntradaAsync(cookie, primero, "L-REPETIDO", Fecha(24), "5")).StatusCode);

        var repetido = await RegistrarEntradaAsync(cookie, primero, "l-repetido", Fecha(24), "5");
        var enOtro = await RegistrarEntradaAsync(cookie, segundo, "L-REPETIDO", Fecha(24), "5");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, repetido.StatusCode);
        Assert.Contains("ya existe para este medicamento", await repetido.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Created, enOtro.StatusCode);
    }

    [Fact]
    public async Task Un_lote_caducado_responde_422_y_no_guarda_nada()
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie);

        var respuesta = await RegistrarEntradaAsync(cookie, id, "L-VIEJO", Fecha(-1), "5");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("No se puede registrar un lote caducado.", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(0, await ContarAsync("SELECT COUNT(*) FROM lote WHERE id_medicamento = @id AND numero_lote = 'L-VIEJO';", id));
    }

    [Fact]
    public async Task Un_medicamento_inexistente_responde_422()
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await RegistrarEntradaAsync(cookie, 2147000000, "L-1", Fecha(24), "5");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("El medicamento seleccionado no existe en el catálogo.", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("L-1", "0", "La cantidad debe ser un número entero mayor que cero.")]
    [InlineData("L-1", "2.5", "La cantidad debe ser un número entero mayor que cero.")]
    [InlineData("", "5", "El número de lote es obligatorio.")]
    [InlineData("L 1 #", "5", "El número de lote solo puede contener letras, números y guiones.")]
    public async Task Datos_incorrectos_responden_400_con_el_mensaje_del_campo(string lote, string cantidad, string mensaje)
    {
        var cookie = await IniciarSesionAsync();
        var (id, _) = await CrearMedicamentoAsync(cookie);

        var respuesta = await RegistrarEntradaAsync(cookie, id, lote, Fecha(24), cantidad);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(mensaje, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task La_busqueda_devuelve_el_medicamento_con_sus_existencias_y_estados()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "7");

        var respuesta = await EnviarAsync(HttpMethod.Get,
            $"/api/entradas-lote/medicamentos?termino={Uri.EscapeDataString(nombre)}", cuerpo: null, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var fila = Assert.Single(lista.EnumerateArray());
        Assert.Equal(id, fila.GetProperty("idMedicamento").GetInt32());
        Assert.Equal(7, fila.GetProperty("existenciasTotales").GetInt32());
        Assert.False(fila.GetProperty("estaAgotado").GetBoolean());
    }

    [Fact]
    public async Task La_busqueda_con_porcentaje_no_devuelve_todo_el_catalogo()
    {
        var cookie = await IniciarSesionAsync();
        await CrearMedicamentoAsync(cookie);

        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/entradas-lote/medicamentos?termino=%25", cuerpo: null, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, lista.GetArrayLength());
    }
}
