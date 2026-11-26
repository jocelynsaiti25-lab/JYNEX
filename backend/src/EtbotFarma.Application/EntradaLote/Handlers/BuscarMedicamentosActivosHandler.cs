using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EntradaLote.Dtos;
using EtbotFarma.Application.EntradaLote.Queries;

namespace EtbotFarma.Application.EntradaLote.Handlers;

/// <summary>
/// HU-07 escenarios 1, 2 y 11: devuelve medicamentos activos con sus existencias y semáforo actual.
/// </summary>
public sealed class BuscarMedicamentosActivosHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj) : IQueryHandler<BuscarMedicamentosActivosQuery, IReadOnlyList<MedicamentoActivoDto>>
{
    public async Task<IReadOnlyList<MedicamentoActivoDto>> Handle(
        BuscarMedicamentosActivosQuery query,
        CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        var hoy = reloj.Hoy;
        var lista = await medicamentos.BuscarActivosAsync(query.Termino, cancellationToken);
        var lotesDeTodos = await lotes.ObtenerPorMedicamentosAsync(lista.Select(m => m.Id).ToList(), cancellationToken);
        var lotesPorMedicamento = lotesDeTodos.ToLookup(l => l.IdMedicamento);

        return lista
            .Select(med =>
            {
                var existencias = med.CalcularExistencias(lotesPorMedicamento[med.Id], hoy);
                return new MedicamentoActivoDto(
                    med.Id,
                    med.NombreComercial,
                    med.NombreSustancia,
                    med.Presentacion,
                    med.Concentracion,
                    med.EsControlado,
                    med.StockMinimo,
                    existencias.Total,
                    existencias.SemaforoGeneral,
                    existencias.EstaAgotado,
                    existencias.EnStockBajo);
            })
            .ToList();
    }
}
