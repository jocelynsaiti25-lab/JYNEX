using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.RegistroMedicamentos;

/// <summary>
/// HU-06 de punta a punta dentro de .NET:
/// Controller → MediatR → Validator → Handler → Repositorios → MySQL.
/// Se marcan con la categoría "Integracion" porque necesitan la base de datos.
/// </summary>
[Trait("Category", "Integracion")]
public class RegistroMedicamentosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    /// <summary>Terminación distinta en cada ejecución para no chocar con medicamentos de pruebas anteriores.</summary>
    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private static string Fecha(int meses) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(meses).ToString("yyyy-MM-dd");

    private static object Datos(string sufijo, string fechaCaducidad, string cantidad = "100", string numeroLote = "L-001") => new
    {
        nombreComercial = $"Paracetamol {sufijo}",
        nombreSustancia = "Paracetamol",
        presentacion = "Tabletas",
        concentracion = "500 mg",
        esControlado = false,
        numeroLote,
        fechaCaducidad,
        cantidadInicial = cantidad
    };

    /// <summary>Inicia sesión y devuelve la cookie ("etbotfarma_sesion=...") para mandarla en las peticiones.</summary>
    private async Task<string> IniciarSesionAsync()
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar",
            new { nombreUsuario = "admin", contrasenia = ContraseniaAdmin });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private async Task<HttpResponseMessage> RegistrarAsync(object datos, string? cookie)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/medicamentos") { Content = JsonContent.Create(datos) };
        if (cookie is not null)
        {
            peticion.Headers.Add("Cookie", cookie);
        }
        return await api.CrearCliente().SendAsync(peticion);
    }

    /// <summary>Consulta directa a MySQL (solo en la prueba): cuántas filas devuelve el SQL con un parámetro.</summary>
    private async Task<long> ContarAsync(string sql, int idMedicamento)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", idMedicamento);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Sin_sesion_responde_401()
    {
        var respuesta = await RegistrarAsync(Datos(Unico(), Fecha(24)), cookie: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Registra_el_medicamento_con_su_lote_y_la_entrada_en_la_bitacora()
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await RegistrarAsync(Datos(Unico(), Fecha(24), cantidad: "100"), cookie);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, json.GetProperty("stock").GetInt32());
        Assert.Equal("Verde", json.GetProperty("semaforoCaducidad").GetString());
        Assert.True(json.GetProperty("seRegistroEntrada").GetBoolean());

        // Quedó guardado: un lote con 100 unidades y un movimiento ENTRADA
        var id = json.GetProperty("idMedicamento").GetInt32();
        Assert.Equal(1, await ContarAsync("SELECT COUNT(*) FROM lote WHERE id_medicamento = @id AND stock = 100;", id));
        Assert.Equal(1, await ContarAsync(
            "SELECT COUNT(*) FROM movimiento WHERE id_medicamento = @id AND tipo = 'ENTRADA' AND cantidad_total = 100;", id));
    }

    [Fact]
    public async Task Con_cantidad_cero_registra_el_lote_pero_no_la_entrada()
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await RegistrarAsync(Datos(Unico(), Fecha(24), cantidad: "0"), cookie);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("seRegistroEntrada").GetBoolean());

        var id = json.GetProperty("idMedicamento").GetInt32();
        Assert.Equal(1, await ContarAsync("SELECT COUNT(*) FROM lote WHERE id_medicamento = @id AND stock = 0;", id));
        Assert.Equal(0, await ContarAsync("SELECT COUNT(*) FROM movimiento WHERE id_medicamento = @id;", id));
    }

    /// <summary>Semáforo: rojo hasta 3 meses, amarillo hasta 12 y verde después.</summary>
    [Theory]
    [InlineData(2, "Rojo")]
    [InlineData(8, "Amarillo")]
    [InlineData(24, "Verde")]
    public async Task El_semaforo_depende_de_los_meses_para_caducar(int meses, string esperado)
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await RegistrarAsync(Datos(Unico(), Fecha(meses)), cookie);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(esperado, json.GetProperty("semaforoCaducidad").GetString());
    }

    [Fact]
    public async Task Un_lote_caducado_responde_422_y_no_guarda_nada()
    {
        var cookie = await IniciarSesionAsync();
        var sufijo = Unico();

        var respuesta = await RegistrarAsync(Datos(sufijo, Fecha(-1)), cookie);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("No se puede registrar un lote caducado.", await respuesta.Content.ReadAsStringAsync());

        // Si se vuelve a intentar con una fecha buena, el medicamento no quedó a medias en la base
        var segundo = await RegistrarAsync(Datos(sufijo, Fecha(24)), cookie);
        Assert.Equal(HttpStatusCode.Created, segundo.StatusCode);
    }

    [Fact]
    public async Task Un_medicamento_repetido_responde_422_aunque_cambien_las_mayusculas()
    {
        var cookie = await IniciarSesionAsync();
        var sufijo = Unico();
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(Datos(sufijo, Fecha(24)), cookie)).StatusCode);

        var repetido = new
        {
            nombreComercial = $"PARACETAMOL {sufijo}",
            nombreSustancia = "paracetamol",
            presentacion = "TABLETAS",
            concentracion = "500 MG",
            esControlado = false,
            numeroLote = "L-002",
            fechaCaducidad = Fecha(24),
            cantidadInicial = "10"
        };
        var respuesta = await RegistrarAsync(repetido, cookie);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("El medicamento ya está registrado.", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Datos_vacios_responden_400_con_los_errores_del_validator()
    {
        var cookie = await IniciarSesionAsync();
        var vacios = new
        {
            nombreComercial = "",
            nombreSustancia = "",
            presentacion = "",
            concentracion = "",
            esControlado = false,
            numeroLote = "",
            fechaCaducidad = "",
            cantidadInicial = ""
        };

        var respuesta = await RegistrarAsync(vacios, cookie);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Este campo es obligatorio.", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("31-12-2030", "100", "L-001", "Fecha de caducidad no válida.")]
    [InlineData("2030-02-31", "100", "L-001", "Fecha de caducidad no válida.")]
    [InlineData("2030-12-31", "-5", "L-001", "La cantidad debe ser un número entero de 0 o más.")]
    [InlineData("2030-12-31", "abc", "L-001", "La cantidad debe ser un número entero de 0 o más.")]
    [InlineData("2030-12-31", "100", "L 001 #", "El número de lote solo puede tener letras, números y guiones.")]
    public async Task Formatos_incorrectos_responden_400_con_el_mensaje_del_campo(
        string fecha, string cantidad, string lote, string mensaje)
    {
        var cookie = await IniciarSesionAsync();

        var respuesta = await RegistrarAsync(Datos(Unico(), fecha, cantidad, lote), cookie);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(mensaje, await respuesta.Content.ReadAsStringAsync());
    }
}
