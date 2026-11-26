using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Historias.EdicionMedicamentos;

[Trait("Category", "Integracion")]
public class EdicionMedicamentosTransaccionTests(
    ApiConMySql api,
    ApiConAuditoriaQueFalla apiAuditoriaFalla,
    ApiConActualizacionQueFalla apiActualizacionFalla)
    : IClassFixture<ApiConMySql>, IClassFixture<ApiConAuditoriaQueFalla>, IClassFixture<ApiConActualizacionQueFalla>
{
    private static readonly string ContraseniaAdmin =
        Environment.GetEnvironmentVariable("AdministradorInicial__Contrasenia") ?? "Prueba123!";

    private static string Unico() => Guid.NewGuid().ToString("N")[..8];

    private async Task<string> IniciarSesionAdminAsync()
    {
        var respuesta = await api.CrearCliente().PostAsJsonAsync("/api/sesiones/iniciar",
            new { nombreUsuario = "admin", contrasenia = ContraseniaAdmin });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, HttpMethod metodo, string url, string cookie, object? cuerpo = null)
    {
        var peticion = new HttpRequestMessage(metodo, url) { Content = cuerpo is null ? null : JsonContent.Create(cuerpo) };
        peticion.Headers.Add("Cookie", cookie);
        return await cliente.SendAsync(peticion);
    }

    private async Task<(int Id, string Sufijo)> RegistrarMedicamentoAsync(string cookie)
    {
        var sufijo = Unico();
        var respuesta = await EnviarAsync(api.CrearCliente(), HttpMethod.Post, "/api/medicamentos", cookie, new
        {
            nombreComercial = $"Transaccion {sufijo}",
            nombreSustancia = "Paracetamol",
            presentacion = "Tabletas",
            concentracion = "500 mg",
            esControlado = false,
            numeroLote = "L-TRANS-001",
            fechaCaducidad = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(24).ToString("yyyy-MM-dd"),
            cantidadInicial = "30"
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("idMedicamento").GetInt32(), sufijo);
    }

    private async Task<string> ConsultarAsync(string sql, int id)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var conexion = scope.ServiceProvider.GetRequiredService<ConexionMySql>();
        await using var comando = await conexion.CrearComandoAsync(sql, CancellationToken.None);
        comando.Parameters.AddWithValue("@id", id);
        return Convert.ToString(await comando.ExecuteScalarAsync()) ?? string.Empty;
    }

    private static object Cambio(string sufijo) => new
    {
        nombreComercial = $"Transaccion {sufijo} Forte",
        nombreSustancia = "Paracetamol",
        presentacion = "Jarabe",
        concentracion = "250 mg/5 ml",
        esControlado = true
    };

    private async Task AfirmarSinCambiosAsync(int id, string sufijo)
    {
        Assert.Equal($"Transaccion {sufijo}", await ConsultarAsync("SELECT nombre_comercial FROM medicamento WHERE id_medicamento = @id;", id));
        Assert.Equal("Tabletas", await ConsultarAsync("SELECT presentacion FROM medicamento WHERE id_medicamento = @id;", id));
        Assert.Equal("0", await ConsultarAsync("SELECT es_controlado FROM medicamento WHERE id_medicamento = @id;", id));
        Assert.Equal("0", await ConsultarAsync(
            "SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", id));
        Assert.Equal("30", await ConsultarAsync("SELECT stock FROM lote WHERE id_medicamento = @id;", id));
    }

    [Fact]
    public async Task Edicion_correcta_deja_el_medicamento_y_la_auditoria_registrados()
    {
        var admin = await IniciarSesionAdminAsync();
        var (id, sufijo) = await RegistrarMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(api.CrearCliente(), HttpMethod.Put, $"/api/medicamentos/{id}", admin, Cambio(sufijo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal($"Transaccion {sufijo} Forte", await ConsultarAsync("SELECT nombre_comercial FROM medicamento WHERE id_medicamento = @id;", id));
        Assert.Equal("1", await ConsultarAsync(
            "SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", id));
    }

    [Fact]
    public async Task Si_falla_la_auditoria_la_edicion_ya_escrita_se_revierte()
    {
        var admin = await IniciarSesionAdminAsync();
        var (id, sufijo) = await RegistrarMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(apiAuditoriaFalla.CrearCliente(), HttpMethod.Put, $"/api/medicamentos/{id}", admin, Cambio(sufijo));

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        await AfirmarSinCambiosAsync(id, sufijo);
    }

    [Fact]
    public async Task Si_falla_la_actualizacion_despues_de_escribir_no_queda_nada_confirmado()
    {
        var admin = await IniciarSesionAdminAsync();
        var (id, sufijo) = await RegistrarMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(apiActualizacionFalla.CrearCliente(), HttpMethod.Put, $"/api/medicamentos/{id}", admin, Cambio(sufijo));

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        await AfirmarSinCambiosAsync(id, sufijo);
    }

    [Fact]
    public async Task Un_cambio_solo_de_capitalizacion_se_guarda_y_se_audita()
    {
        var admin = await IniciarSesionAdminAsync();
        var (id, sufijo) = await RegistrarMedicamentoAsync(admin);

        var respuesta = await EnviarAsync(api.CrearCliente(), HttpMethod.Put, $"/api/medicamentos/{id}", admin, new
        {
            nombreComercial = $"Transaccion {sufijo}",
            nombreSustancia = "PARACETAMOL",
            presentacion = "TABLETAS",
            concentracion = "500 MG",
            esControlado = false
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("seGuardaronCambios").GetBoolean());
        Assert.Equal("TABLETAS", await ConsultarAsync("SELECT presentacion FROM medicamento WHERE id_medicamento = @id;", id));
        Assert.Equal("1", await ConsultarAsync(
            "SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = @id;", id));
    }
}
