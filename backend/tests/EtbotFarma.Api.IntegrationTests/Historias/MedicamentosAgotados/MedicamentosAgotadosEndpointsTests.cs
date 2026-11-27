using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.MedicamentosAgotados;

[Trait("Category", "Integracion")]
public class MedicamentosAgotadosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private static string Fecha(int meses) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(meses).ToString("yyyy-MM-dd");

    private async Task<string> IniciarSesionAsync(string usuario = "admin", string? contrasenia = null)
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar",
            new { nombreUsuario = usuario, contrasenia = contrasenia ?? ContraseniaAdmin });
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

    private async Task<(int Id, string Nombre)> CrearMedicamentoAsync(
        string cookie, string cantidadInicial, string presentacion = "Tabletas", string concentracion = "500 mg", string? nombre = null)
    {
        nombre ??= $"Agotado {Unico()}";
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/medicamentos", new
        {
            nombreComercial = nombre,
            nombreSustancia = "Sustancia de prueba",
            presentacion,
            concentracion,
            esControlado = false,
            numeroLote = "L-INICIAL",
            fechaCaducidad = Fecha(24),
            cantidadInicial
        }, cookie);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("idMedicamento").GetInt32(), nombre);
    }

    private async Task EjecutarSqlAsync(string sql, int idMedicamento)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", idMedicamento);
        await comando.ExecuteNonQueryAsync();
        await conexion.GuardarCambiosAsync(CancellationToken.None);
    }

    private async Task<JsonElement[]> ConsultarAgotadosAsync(string cookie, string? termino = null)
    {
        var ruta = termino is null ? "/api/inventario/agotados" : $"/api/inventario/agotados?termino={Uri.EscapeDataString(termino)}";
        var respuesta = await EnviarAsync(HttpMethod.Get, ruta, cuerpo: null, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var lista = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return lista.EnumerateArray().ToArray();
    }

    private static bool Contiene(JsonElement[] lista, int id) =>
        lista.Any(f => f.GetProperty("idMedicamento").GetInt32() == id);

    [Fact]
    public async Task Sin_sesion_responde_401_y_no_devuelve_datos()
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, "/api/inventario/agotados", cuerpo: null, cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Medicamento_registrado_con_cero_unidades_aparece_con_la_etiqueta_y_sus_datos()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "0", presentacion: "Jarabe", concentracion: "120 mg/5 ml");

        var lista = await ConsultarAgotadosAsync(cookie, nombre);

        var fila = Assert.Single(lista);
        Assert.Equal(id, fila.GetProperty("idMedicamento").GetInt32());
        Assert.Equal(nombre, fila.GetProperty("nombreComercial").GetString());
        Assert.Equal("Jarabe", fila.GetProperty("presentacion").GetString());
        Assert.Equal("120 mg/5 ml", fila.GetProperty("concentracion").GetString());
        Assert.Equal("SIN EXISTENCIAS", fila.GetProperty("etiqueta").GetString());
    }

    [Fact]
    public async Task Medicamento_con_existencias_vigentes_no_aparece()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "10");

        var lista = await ConsultarAgotadosAsync(cookie, nombre);

        Assert.False(Contiene(lista, id));
    }

    [Fact]
    public async Task Medicamento_con_solo_un_lote_caducado_con_unidades_aparece_como_agotado()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "10");

        await EjecutarSqlAsync(
            "UPDATE lote SET fecha_caducidad = DATE_SUB(CURDATE(), INTERVAL 3 DAY) WHERE id_medicamento = @id;", id);

        var lista = await ConsultarAgotadosAsync(cookie, nombre);

        Assert.True(Contiene(lista, id));
    }

    [Fact]
    public async Task Un_lote_que_caduca_en_dos_meses_sigue_contando_como_existencia()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "10");

        await EjecutarSqlAsync(
            "UPDATE lote SET fecha_caducidad = DATE_ADD(CURDATE(), INTERVAL 2 MONTH) WHERE id_medicamento = @id;", id);

        var lista = await ConsultarAgotadosAsync(cookie, nombre);

        Assert.False(Contiene(lista, id));
    }

    [Fact]
    public async Task Medicamento_con_el_mismo_nombre_y_otra_presentacion_con_existencias_no_aparece()
    {
        var cookie = await IniciarSesionAsync();
        var nombre = $"Paracetamol {Unico()}";
        var (idAgotado, _) = await CrearMedicamentoAsync(cookie, "0", "Tabletas", "500 mg", nombre);
        var (idConStock, _) = await CrearMedicamentoAsync(cookie, "20", "Jarabe", "120 mg/5 ml", nombre);

        var lista = await ConsultarAgotadosAsync(cookie, nombre);

        Assert.True(Contiene(lista, idAgotado));
        Assert.False(Contiene(lista, idConStock));
    }

    [Fact]
    public async Task Una_entrada_con_unidades_lo_saca_de_agotados_y_una_con_cero_no_se_acepta()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "0");
        Assert.True(Contiene(await ConsultarAgotadosAsync(cookie, nombre), id));

        var entradaVacia = await EnviarAsync(HttpMethod.Post, "/api/entradas-lote",
            new { idMedicamento = id, numeroLote = "L-CERO", fechaCaducidad = Fecha(18), cantidad = "0" }, cookie);
        Assert.NotEqual(HttpStatusCode.Created, entradaVacia.StatusCode);
        Assert.True(Contiene(await ConsultarAgotadosAsync(cookie, nombre), id));

        var entrada = await EnviarAsync(HttpMethod.Post, "/api/entradas-lote",
            new { idMedicamento = id, numeroLote = "L-CON-STOCK", fechaCaducidad = Fecha(18), cantidad = "15" }, cookie);
        Assert.Equal(HttpStatusCode.Created, entrada.StatusCode);
        Assert.False(Contiene(await ConsultarAgotadosAsync(cookie, nombre), id));
    }

    [Fact]
    public async Task La_busqueda_se_combina_con_el_filtro_y_si_nada_coincide_la_lista_queda_vacia()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "0");

        Assert.Single(await ConsultarAgotadosAsync(cookie, nombre[..^2]));
        Assert.Empty(await ConsultarAgotadosAsync(cookie, $"ZZZ-{Unico()}"));
        Assert.True(Contiene(await ConsultarAgotadosAsync(cookie), id));
    }

    [Fact]
    public async Task Consultar_varias_veces_no_cambia_las_existencias_ni_los_lotes()
    {
        var cookie = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookie, cantidadInicial: "0");

        await ConsultarAgotadosAsync(cookie, nombre);
        await ConsultarAgotadosAsync(cookie, nombre);

        var inventario = await EnviarAsync(HttpMethod.Get, $"/api/inventario?termino={Uri.EscapeDataString(nombre)}", cuerpo: null, cookie);
        var filas = (await inventario.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        var fila = Assert.Single(filas);
        Assert.Equal(id, fila.GetProperty("idMedicamento").GetInt32());
        Assert.Equal(0, fila.GetProperty("existencias").GetInt32());
    }

    [Fact]
    public async Task El_Empleado_tambien_puede_consultar_los_agotados()
    {
        var cookieAdmin = await IniciarSesionAsync();
        var (id, nombre) = await CrearMedicamentoAsync(cookieAdmin, cantidadInicial: "0");

        var sufijo = Unico();
        var usuario = $"emp.agot.{sufijo}";
        var alta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", new
        {
            nombre = "Luis",
            apellidos = "Prueba",
            nombreUsuario = usuario,
            correo = $"{usuario}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia = "Farmacia#2026",
            idRol = 2
        }, cookieAdmin);
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);

        var cookieEmpleado = await IniciarSesionAsync(usuario, "Farmacia#2026");
        var lista = await ConsultarAgotadosAsync(cookieEmpleado, nombre);

        Assert.True(Contiene(lista, id));
    }
}
