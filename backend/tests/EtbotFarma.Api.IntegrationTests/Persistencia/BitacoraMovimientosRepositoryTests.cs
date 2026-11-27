using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Persistencia;

[Trait("Category", "Integracion")]
public class BitacoraMovimientosRepositoryTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    [Fact]
    public async Task Pagina_movimientos_completos_y_aplica_filtro_de_tipo()
    {
        var sufijo = Guid.NewGuid().ToString("N")[..10];
        int idMedicamento;
        int idEntrada;
        int idSalida;
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var instante = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 12, 0, 0, DateTimeKind.Utc);

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var proveedor = scope.ServiceProvider;
            var medicamentos = proveedor.GetRequiredService<IMedicamentoRepository>();
            var lotes = proveedor.GetRequiredService<ILoteRepository>();
            var movimientos = proveedor.GetRequiredService<IMovimientoRepository>();
            var usuarios = proveedor.GetRequiredService<IUsuarioRepository>();
            var unidadDeTrabajo = proveedor.GetRequiredService<IUnitOfWork>();
            var admin = await usuarios.ObtenerPorNombreUsuarioAsync("admin", CancellationToken.None);
            Assert.NotNull(admin);

            var medicamento = Medicamento.Crear(
                $"US15 {sufijo}", $"Sustancia {sufijo}", "Tabletas", "500 mg", false, instante);
            idMedicamento = await medicamentos.AgregarAsync(medicamento, CancellationToken.None);

            var loteUno = Lote.CrearEntrada(idMedicamento, $"A-{sufijo}", hoy.AddMonths(12), 50, hoy);
            var loteDos = Lote.CrearEntrada(idMedicamento, $"B-{sufijo}", hoy.AddMonths(15), 50, hoy);
            var idLoteUno = await lotes.AgregarAsync(loteUno, CancellationToken.None);
            var idLoteDos = await lotes.AgregarAsync(loteDos, CancellationToken.None);

            var entrada = Movimiento.CrearEntradaLote(idMedicamento, idLoteUno, admin.Id, 50, instante);
            idEntrada = await movimientos.AgregarConDetallesAsync(entrada, CancellationToken.None);
            var salida = Movimiento.CrearSalida(idMedicamento, admin.Id,
            [
                new AsignacionLote(idLoteUno, loteUno.NumeroLote, loteUno.FechaCaducidad, 20),
                new AsignacionLote(idLoteDos, loteDos.NumeroLote, loteDos.FechaCaducidad, 10)
            ], instante);
            idSalida = await movimientos.AgregarConDetallesAsync(salida, CancellationToken.None);
            await unidadDeTrabajo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using var consultaScope = api.Services.CreateAsyncScope();
        var repositorio = consultaScope.ServiceProvider.GetRequiredService<IMovimientoRepository>();
        var paginaUno = await repositorio.ConsultarAsync(
            new FiltroBitacoraDto(instante.Date, instante.Date.AddDays(1), null, idMedicamento, null, 1, 1),
            CancellationToken.None);
        var paginaDos = await repositorio.ConsultarAsync(
            new FiltroBitacoraDto(instante.Date, instante.Date.AddDays(1), null, idMedicamento, null, 2, 1),
            CancellationToken.None);
        var soloSalidas = await repositorio.ConsultarAsync(
            new FiltroBitacoraDto(null, null, null, idMedicamento, Movimiento.TipoSalida, 1, 10),
            CancellationToken.None);

        var salidaPaginada = Assert.Single(paginaUno.Movimientos);
        Assert.Equal(idSalida, salidaPaginada.IdMovimiento);
        Assert.Equal(Movimiento.TipoSalida, salidaPaginada.Tipo);
        Assert.Equal(30, salidaPaginada.CantidadTotal);
        Assert.Equal(2, salidaPaginada.Detalles.Count);
        Assert.Equal(2, paginaUno.TotalMovimientos);

        var entradaPaginada = Assert.Single(paginaDos.Movimientos);
        Assert.Equal(idEntrada, entradaPaginada.IdMovimiento);
        Assert.Single(entradaPaginada.Detalles);
        Assert.Equal(2, paginaDos.TotalPaginas);

        Assert.Equal(1, soloSalidas.TotalMovimientos);
        Assert.Equal(Movimiento.TipoSalida, Assert.Single(soloSalidas.Movimientos).Tipo);
    }
}
