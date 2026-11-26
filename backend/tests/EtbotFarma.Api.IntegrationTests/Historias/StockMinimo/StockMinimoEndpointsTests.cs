using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

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

    private async Task<string> CrearEmpleadoAsync(string admin)
    {
        var nombreUsuario = $"stock.{Unico()}";
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

        var inicio = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario, contrasenia });
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
        return inicio.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private async Task<JsonElement> ObtenerItemAsync(string cookie, int idMedicamento)
    {
        var lista = await EnviarAsync(HttpMethod.Get, "/api/medicamentos/stock-minimo", cuerpo: null, cookie);
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var items = await lista.Content.ReadFromJsonAsync<JsonElement>();
        return items.EnumerateArray().Single(i => i.GetProperty("idMedicamento").GetInt32() == idMedicamento);
    }

    private async Task<HttpResponseMessage> EnviarJsonCrudoAsync(string ruta, string json, string cookie)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Put, ruta)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        peticion.Headers.Add("Cookie", cookie);
        return await api.CrearCliente().SendAsync(peticion);
    }

    private async Task CaducarLotesAsync(int idMedicamento)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using (var comando = await conexion.CrearComandoAsync(
            "UPDATE lote SET fecha_caducidad = @fecha WHERE id_medicamento = @id;", CancellationToken.None))
        {
            comando.Parameters.AddWithValue("@fecha", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd"));
            comando.Parameters.AddWithValue("@id", idMedicamento);
            await comando.ExecuteNonQueryAsync();
        }
        await conexion.GuardarCambiosAsync(CancellationToken.None);
    }

    private async Task<int?> LeerStockMinimoEnBaseAsync(int idMedicamento)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(
            "SELECT stock_minimo FROM medicamento WHERE id_medicamento = @id;", CancellationToken.None);
        comando.Parameters.AddWithValue("@id", idMedicamento);
        var valor = await comando.ExecuteScalarAsync();
        return valor is null ? null : Convert.ToInt32(valor);
    }

    [Fact]
    public async Task Administrador_consulta_el_listado_con_existencias_y_sin_alertas_de_caducidad()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);

        var item = await ObtenerItemAsync(admin, idMedicamento);

        Assert.Equal(10, item.GetProperty("existenciasVigentes").GetInt32());
        Assert.Equal(0, item.GetProperty("existenciasCaducadas").GetInt32());
        Assert.False(item.GetProperty("tieneLotesCaducados").GetBoolean());
        Assert.False(item.GetProperty("estaAgotado").GetBoolean());
    }

    [Fact]
    public async Task Empleado_consulta_el_listado()
    {
        var admin = await IniciarSesionAsync();
        var empleado = await CrearEmpleadoAsync(admin);
        var idMedicamento = await CrearMedicamentoAsync(admin);

        var item = await ObtenerItemAsync(empleado, idMedicamento);

        Assert.Equal(10, item.GetProperty("existenciasVigentes").GetInt32());
    }

    [Fact]
    public async Task Empleado_modifica_el_stock_minimo_y_queda_guardado_en_mysql()
    {
        var admin = await IniciarSesionAsync();
        var empleado = await CrearEmpleadoAsync(admin);
        var idMedicamento = await CrearMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(
            HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = 12 }, empleado);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(12, await LeerStockMinimoEnBaseAsync(idMedicamento));
        var item = await ObtenerItemAsync(empleado, idMedicamento);
        Assert.Equal(12, item.GetProperty("stockMinimo").GetInt32());
        Assert.True(item.GetProperty("enStockBajo").GetBoolean());
    }

    [Fact]
    public async Task Administrador_modifica_el_stock_minimo_y_queda_guardado_en_mysql()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(
            HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = 4 }, admin);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(4, await LeerStockMinimoEnBaseAsync(idMedicamento));
    }

    [Fact]
    public async Task Cero_es_un_valor_valido_y_se_guarda()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        await EnviarAsync(HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = 8 }, admin);

        var respuesta = await EnviarAsync(
            HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = 0 }, admin);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(0, await LeerStockMinimoEnBaseAsync(idMedicamento));
    }

    [Fact]
    public async Task Sin_sesion_no_puede_modificar_el_stock_minimo_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(
            HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = 99 }, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal(0, await LeerStockMinimoEnBaseAsync(idMedicamento));
    }

    [Fact]
    public async Task Empleado_con_valor_negativo_recibe_400_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync();
        var empleado = await CrearEmpleadoAsync(admin);
        var idMedicamento = await CrearMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(
            HttpMethod.Put, $"/api/medicamentos/{idMedicamento}/stock-minimo", new { nuevoStockMinimo = -3 }, empleado);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await LeerStockMinimoEnBaseAsync(idMedicamento));
    }

    [Theory]
    [InlineData("{\"nuevoStockMinimo\":5.5}")]
    [InlineData("{\"nuevoStockMinimo\":\"abc\"}")]
    [InlineData("{\"nuevoStockMinimo\":\"\"}")]
    [InlineData("{\"nuevoStockMinimo\":null}")]
    [InlineData("{}")]
    public async Task Datos_que_no_son_un_entero_responden_400_y_no_cambian_nada(string json)
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);

        var respuesta = await EnviarJsonCrudoAsync($"/api/medicamentos/{idMedicamento}/stock-minimo", json, admin);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(0, await LeerStockMinimoEnBaseAsync(idMedicamento));
    }

    [Fact]
    public async Task Los_lotes_caducados_no_cuentan_como_vigentes_pero_se_siguen_avisando()
    {
        var admin = await IniciarSesionAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin);
        await CaducarLotesAsync(idMedicamento);

        var item = await ObtenerItemAsync(admin, idMedicamento);

        Assert.Equal(0, item.GetProperty("existenciasVigentes").GetInt32());
        Assert.Equal(10, item.GetProperty("existenciasCaducadas").GetInt32());
        Assert.True(item.GetProperty("tieneLotesCaducados").GetBoolean());
        Assert.True(item.GetProperty("estaAgotado").GetBoolean());
    }

    [Fact]
    public async Task Empleado_sin_sesion_valida_tras_cerrar_sesion_recibe_401()
    {
        var admin = await IniciarSesionAsync();
        var empleado = await CrearEmpleadoAsync(admin);
        var cierre = await EnviarAsync(HttpMethod.Post, "/api/sesiones/cerrar", cuerpo: null, empleado);
        Assert.True(cierre.IsSuccessStatusCode);

        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/medicamentos/stock-minimo", cuerpo: null, empleado);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
