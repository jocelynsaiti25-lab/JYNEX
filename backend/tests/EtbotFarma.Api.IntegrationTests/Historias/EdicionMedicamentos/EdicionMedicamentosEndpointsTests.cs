using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.EdicionMedicamentos;

/// <summary>
/// HU-09 de punta a punta dentro de .NET:
/// Controller → MediatR → Validator → Handler → Repositorios → MySQL (una sola transacción).
/// Se marcan con la categoría "Integracion" porque necesitan la base de datos.
/// </summary>
[Trait("Category", "Integracion")]
public class EdicionMedicamentosEndpointsTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private static string Fecha(int meses) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(meses).ToString("yyyy-MM-dd");

    private async Task<string> IniciarSesionAsync(string nombreUsuario, string contrasenia)
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar", new { nombreUsuario, contrasenia });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, string? cookie, object? cuerpo = null)
    {
        var peticion = new HttpRequestMessage(metodo, url);
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

    /// <summary>Crea un Empleado nuevo con HU-03 y devuelve su cookie de sesión.</summary>
    private async Task<string> CrearEmpleadoAsync(string admin)
    {
        var nombreUsuario = $"edicion.{Unico()}";
        const string contrasenia = "Farmacia#2026";
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/usuarios", admin, new
        {
            nombre = "Luis",
            apellidos = "Pérez",
            nombreUsuario,
            correo = $"{nombreUsuario}@etbotfarma.com",
            telefono = "5512345678",
            contrasenia,
            idRol = 2
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await IniciarSesionAsync(nombreUsuario, contrasenia);
    }

    /// <summary>Da de alta un medicamento con lote inicial (HU-06) y devuelve su id_medicamento.</summary>
    private async Task<int> RegistrarMedicamentoAsync(string admin, string sufijo, int cantidadInicial = 50)
    {
        var respuesta = await EnviarAsync(HttpMethod.Post, "/api/medicamentos", admin, new
        {
            nombreComercial = $"Paracetamol {sufijo}",
            nombreSustancia = "Paracetamol",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-EDIT-001",
            fechaCaducidad = Fecha(24),
            cantidadInicial = cantidadInicial.ToString()
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("idMedicamento").GetInt32();
    }

    private Task<HttpResponseMessage> EditarAsync(int id, string? cookie, object datos)
        => EnviarAsync(HttpMethod.Put, $"/api/medicamentos/{id}", cookie, datos);

    private async Task<string> ConsultarAsync(string sql, int id)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", id);
        return Convert.ToString(await comando.ExecuteScalarAsync()) ?? string.Empty;
    }

    /// <summary>Escenarios 1, 5, 9 y 11: edición exitosa, auditoría registrada y stock/lotes intactos.</summary>
    [Fact]
    public async Task Edicion_exitosa_actualiza_y_no_toca_stock_ni_lotes()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        var id = await RegistrarMedicamentoAsync(admin, sufijo);

        var respuesta = await EditarAsync(id, admin, new
        {
            nombreComercial = $"Paracetamol {sufijo} Forte",
            nombreSustancia = "Paracetamol",
            presentacion = "Jarabe",
            concentracion = "250 mg/5 ml",
            esControlado = true
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("seGuardaronCambios").GetBoolean());
        Assert.Equal("Jarabe", json.GetProperty("presentacion").GetString());

        // Escenario 11: el lote y su stock no cambiaron
        Assert.Equal("50", await ConsultarAsync("SELECT stock FROM lote WHERE id_medicamento = @id;", id));
        Assert.Equal("1", await ConsultarAsync("SELECT COUNT(*) FROM lote WHERE id_medicamento = @id;", id));

        // Escenario 1: auditoría con antes/después, en menos de 5 minutos
        Assert.Equal("1", await ConsultarAsync(
            "SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND tabla_afectada = 'medicamento' AND id_registro = @id;", id));
        var valores = await ConsultarAsync(
            "SELECT CONCAT(IFNULL(valor_anterior, ''), IFNULL(valor_nuevo, '')) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", id);
        Assert.Contains("Jarabe", valores);
        Assert.Contains("Tabletas", valores);
    }

    /// <summary>Escenarios 3 y 4: sin sesión (401) o Empleado (403) → no se modifica ningún dato.</summary>
    [Fact]
    public async Task Sin_permisos_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var id = await RegistrarMedicamentoAsync(admin, Unico());
        var empleado = await CrearEmpleadoAsync(admin);
        var datos = new { nombreComercial = "Cambiado", nombreSustancia = "Paracetamol", presentacion = "Tabletas", concentracion = "500 mg", esControlado = false };

        Assert.Equal(HttpStatusCode.Unauthorized, (await EditarAsync(id, null, datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await EditarAsync(id, empleado, datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await EnviarAsync(HttpMethod.Get, $"/api/medicamentos/{id}", empleado)).StatusCode);

        Assert.Equal("0", await ConsultarAsync("SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", id));
    }

    /// <summary>Un id que no existe (dirección escrita a mano) → 404 "El medicamento no existe".</summary>
    [Fact]
    public async Task Con_un_id_que_no_existe_responde_404()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);

        var respuesta = await EditarAsync(999999, admin,
            new { nombreComercial = "X", nombreSustancia = "Y", presentacion = "Z", concentracion = "W", esControlado = false });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("El medicamento no existe", await respuesta.Content.ReadAsStringAsync());
    }

    /// <summary>Escenario 8: la combinación ya registrada en OTRO medicamento → 422 y no cambia nada.</summary>
    [Fact]
    public async Task Con_duplicado_en_otro_medicamento_responde_422_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        var idOriginal = await RegistrarMedicamentoAsync(admin, sufijo);
        await RegistrarMedicamentoAsync(admin, $"{sufijo}-dup");

        // Intenta dejar al original IDÉNTICO al segundo medicamento (mismos 4 campos)
        var respuesta = await EditarAsync(idOriginal, admin, new
        {
            nombreComercial = $"Paracetamol {sufijo}-dup",
            nombreSustancia = "Paracetamol",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Contains("Ya existe otro medicamento registrado con esos datos", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("0", await ConsultarAsync("SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", idOriginal));
    }

    /// <summary>Escenario 12: guardar sin ningún cambio real no genera auditoría.</summary>
    [Fact]
    public async Task Sin_cambios_reales_no_registra_auditoria()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        var id = await RegistrarMedicamentoAsync(admin, sufijo);

        var respuesta = await EditarAsync(id, admin, new
        {
            nombreComercial = $"Paracetamol {sufijo}",
            nombreSustancia = "Paracetamol",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("seGuardaronCambios").GetBoolean());
        Assert.Equal("0", await ConsultarAsync("SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", id));
    }

    /// <summary>Escenario 7: campos descriptivos obligatorios vacíos → 400 y no cambia nada.</summary>
    [Fact]
    public async Task Con_campos_obligatorios_vacios_responde_400_y_no_cambia_nada()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var id = await RegistrarMedicamentoAsync(admin, Unico());

        var respuesta = await EditarAsync(id, admin,
            new { nombreComercial = "", nombreSustancia = "", presentacion = "", concentracion = "", esControlado = false });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Este campo es obligatorio.", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal("0", await ConsultarAsync("SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", id));
    }

    /// <summary>Escenario 2: GET precarga los datos descriptivos y las existencias de referencia.</summary>
    [Fact]
    public async Task Obtener_para_editar_precarga_los_datos_actuales()
    {
        var admin = await IniciarSesionAsync("admin", ContraseniaAdmin);
        var sufijo = Unico();
        var id = await RegistrarMedicamentoAsync(admin, sufijo, cantidadInicial: 30);

        var respuesta = await EnviarAsync(HttpMethod.Get, $"/api/medicamentos/{id}", admin);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"Paracetamol {sufijo}", json.GetProperty("nombreComercial").GetString());
        Assert.Equal(30, json.GetProperty("existenciasTotales").GetInt32());
    }
}
