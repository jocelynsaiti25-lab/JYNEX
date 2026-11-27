using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.BitacoraMovimientos;

[Trait("Category", "Integracion")]
public class BitacoraExportacionEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly XNamespace Hoja = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

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

    private async Task<string> CrearEmpleadoYEntrarAsync(string cookieAdmin)
    {
        var sufijo = Unico();
        var nombreUsuario = $"exporta.{sufijo}";
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", new
        {
            nombre = "Rosa",
            apellidos = "Ibarra",
            nombreUsuario,
            correo = $"exporta.{sufijo}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia = "Farmacia#2026",
            idRol = 2
        }, cookieAdmin);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await IniciarSesionAsync(nombreUsuario, "Farmacia#2026");
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
            nombreComercial = $"Exporta {Unico()}",
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

    private async Task DarDeBajaAsync(string cookie, int idLote, int cantidad, string motivo)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, $"/api/inventario/lotes/{idLote}/baja",
            new { cantidadConsultada = cantidad, motivo }, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    private async Task<int> PrepararMedicamentoConHistorialAsync(string cookie, string motivoBaja = "Caducado")
    {
        var idMedicamento = await CrearMedicamentoAsync(cookie, 10);
        var idLoteB = await AgregarLoteAsync(cookie, idMedicamento, "L-B", 10);
        await RegistrarSalidaAsync(cookie, idMedicamento, 15);
        await CaducarLoteAsync(idLoteB);
        await DarDeBajaAsync(cookie, idLoteB, 5, motivoBaja);
        return idMedicamento;
    }

    private async Task<int> TotalEnBitacoraAsync(string cookie, string consulta)
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/bitacora/movimientos?{consulta}&pagina=1&tamanoPagina=1", null, cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("total").GetInt32();
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

    private async Task<(HttpResponseMessage Respuesta, byte[] Contenido)> ExportarAsync(string? cookie, string consulta)
    {
        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/bitacora/movimientos/exportacion?{consulta}", null, cookie);
        return (respuesta, await respuesta.Content.ReadAsByteArrayAsync());
    }

    private static XDocument Hoja1(byte[] archivo)
    {
        using var zip = new ZipArchive(new MemoryStream(archivo), ZipArchiveMode.Read);
        using var flujo = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        return XDocument.Load(flujo);
    }

    private static List<List<XElement>> Filas(byte[] archivo) =>
        Hoja1(archivo).Descendants(Hoja + "row").Select(f => f.Elements(Hoja + "c").ToList()).ToList();

    private static string Texto(XElement celda) => celda.Descendants(Hoja + "t").Single().Value;

    [Theory]
    [InlineData("")]
    [InlineData("tipo=BAJA")]
    public async Task Sin_sesion_responde_401_y_no_entrega_archivo(string consulta)
    {
        var (respuesta, contenido) = await ExportarAsync(null, consulta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.DoesNotContain("spreadsheetml", respuesta.Content.Headers.ContentType?.ToString() ?? string.Empty);
        Assert.True(contenido.Length < 1000);
    }

    [Fact]
    public async Task El_empleado_no_puede_exportar_y_recibe_403()
    {
        var admin = await IniciarSesionAdminAsync();
        var empleado = await CrearEmpleadoYEntrarAsync(admin);

        var (respuesta, _) = await ExportarAsync(empleado, string.Empty);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_administrador_descarga_un_xlsx_con_nombre_descriptivo_y_encabezados()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);

        var (respuesta, contenido) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Matches(@"^bitacora-movimientos-\d{4}-\d{2}-\d{2}\.xlsx$",
            respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? string.Empty);
        Assert.Equal((byte)'P', contenido[0]);
        Assert.Equal((byte)'K', contenido[1]);

        var filas = Filas(contenido);
        Assert.Equal(
            new[] { "Fecha", "Hora", "Usuario", "Tipo de movimiento", "Medicamento", "Lote", "Cantidad", "Motivo" },
            filas[0].Select(Texto).ToArray());
        Assert.Equal(5, filas.Count);
    }

    [Fact]
    public async Task El_archivo_trae_los_movimientos_con_tipos_de_dato_utilizables_y_el_detalle_de_lotes()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);

        var (_, contenido) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}");

        var filas = Filas(contenido);
        var baja = filas[1];
        var salida = filas[2];

        Assert.Equal("BAJA", Texto(baja[3]));
        Assert.Equal("Caducado", Texto(baja[7]));
        Assert.Equal("L-B (5)", Texto(baja[5]));
        Assert.Equal("SALIDA", Texto(salida[3]));
        Assert.Equal("L-A (10); L-B (5)", Texto(salida[5]));
        Assert.Equal(string.Empty, Texto(salida[7]));

        foreach (var fila in filas.Skip(1))
        {
            Assert.Null(fila[0].Attribute("t"));
            Assert.True(double.Parse(fila[0].Element(Hoja + "v")!.Value, System.Globalization.CultureInfo.InvariantCulture) > 45000);
            Assert.Null(fila[1].Attribute("t"));
            Assert.Null(fila[6].Attribute("t"));
            Assert.False(string.IsNullOrWhiteSpace(Texto(fila[2])));
        }

        Assert.Equal("5", baja[6].Element(Hoja + "v")!.Value);
        Assert.Equal("15", salida[6].Element(Hoja + "v")!.Value);
    }

    [Fact]
    public async Task El_archivo_respeta_los_filtros_y_coincide_con_el_total_de_la_bitacora()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);

        var (_, soloBajas) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}&tipo=BAJA");
        var (_, entradas) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}&tipo=entrada&desde={Dia(-1)}&hasta={Dia(1)}");
        var (_, todos) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}");

        Assert.Equal(2, Filas(soloBajas).Count);
        Assert.Equal(3, Filas(entradas).Count);
        Assert.All(Filas(entradas).Skip(1), fila => Assert.Equal("ENTRADA", Texto(fila[3])));
        Assert.Equal(await TotalEnBitacoraAsync(admin, $"idMedicamento={idMedicamento}"), Filas(todos).Count - 1);
        Assert.Equal(await TotalEnBitacoraAsync(admin, $"idMedicamento={idMedicamento}&tipo=BAJA"), Filas(soloBajas).Count - 1);
    }

    [Fact]
    public async Task Sin_filtros_incluye_los_movimientos_de_todos_los_medicamentos()
    {
        var admin = await IniciarSesionAdminAsync();
        await PrepararMedicamentoConHistorialAsync(admin);
        await PrepararMedicamentoConHistorialAsync(admin);

        var (respuesta, contenido) = await ExportarAsync(admin, string.Empty);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(Filas(contenido).Count - 1 >= 8);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://malicioso\",\"clic\")")]
    [InlineData("+1+1")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public async Task Un_motivo_que_parece_formula_se_exporta_como_texto(string motivo)
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin, motivo);

        var (_, contenido) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}&tipo=BAJA");

        var celda = Filas(contenido)[1][7];
        Assert.Empty(Hoja1(contenido).Descendants(Hoja + "f"));
        Assert.Equal("inlineStr", celda.Attribute("t")!.Value);
        Assert.Equal(motivo, Texto(celda));
    }

    [Fact]
    public async Task Los_acentos_y_caracteres_especiales_se_conservan()
    {
        var admin = await IniciarSesionAdminAsync();
        const string motivo = "Envase dañado: ñandú & café <ok> 5% — ¿revisar?";
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin, motivo);

        var (_, contenido) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}&tipo=BAJA");

        Assert.Equal(motivo, Texto(Filas(contenido)[1][7]));
    }

    [Fact]
    public async Task Sin_movimientos_informa_y_no_entrega_archivo()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await CrearMedicamentoAsync(admin, 10);

        var (respuesta, contenido) = await ExportarAsync(admin, $"idMedicamento={idMedicamento}&tipo=BAJA");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.NotEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("No hay movimientos para exportar.", System.Text.Encoding.UTF8.GetString(contenido));
    }

    [Theory]
    [InlineData("desde=2026-05-11&hasta=2026-05-10")]
    [InlineData("desde=1999-01-01")]
    [InlineData("tipo=TRASLADO")]
    [InlineData("idUsuario=0")]
    [InlineData("idMedicamento=-1")]
    [InlineData("desde=no-es-fecha")]
    public async Task Parametros_no_validos_responden_400_sin_archivo(string consulta)
    {
        var admin = await IniciarSesionAdminAsync();

        var (respuesta, _) = await ExportarAsync(admin, consulta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.NotEqual("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Exportar_no_cambia_datos_ni_crea_movimientos()
    {
        var admin = await IniciarSesionAdminAsync();
        var idMedicamento = await PrepararMedicamentoConHistorialAsync(admin);
        var antes = await ContarMovimientosAsync(idMedicamento);

        await ExportarAsync(admin, $"idMedicamento={idMedicamento}");
        await ExportarAsync(admin, $"idMedicamento={idMedicamento}&tipo=SALIDA");

        Assert.Equal(antes, await ContarMovimientosAsync(idMedicamento));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task La_exportacion_solo_acepta_consultas_de_lectura(string metodo)
    {
        var admin = await IniciarSesionAdminAsync();

        var respuesta = await EnviarAsync(new HttpMethod(metodo), "/api/bitacora/movimientos/exportacion", new { }, admin);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);
    }
}
