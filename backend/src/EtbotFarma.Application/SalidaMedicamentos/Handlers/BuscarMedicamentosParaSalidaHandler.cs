using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using EtbotFarma.Domain.ValueObjects;

namespace EtbotFarma.Application.SalidaMedicamentos.Handlers;

public sealed class BuscarMedicamentosParaSalidaHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj) : IQueryHandler<BuscarMedicamentosParaSalidaQuery, IReadOnlyList<MedicamentoParaSalidaDto>>
{
    public async Task<IReadOnlyList<MedicamentoParaSalidaDto>> Handle(
        BuscarMedicamentosParaSalidaQuery query,
        CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        var hoy = reloj.Hoy;
        var termino = query.Termino?.Trim();

        var coincidencias = (await medicamentos.ObtenerTodosActivosAsync(cancellationToken))
            .Where(m => string.IsNullOrWhiteSpace(termino)
                || m.NombreComercial.Contains(termino, StringComparison.OrdinalIgnoreCase)
                || m.NombreSustancia.Contains(termino, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var todosLosLotes = await lotes.ObtenerPorMedicamentosAsync(
            coincidencias.Select(m => m.Id).ToList(), cancellationToken);
        var lotesPorMedicamento = todosLosLotes.ToLookup(l => l.IdMedicamento);

        return coincidencias
            .Select(m =>
            {
                var lotesMedicamento = lotesPorMedicamento[m.Id].ToList();
                var existencias = m.CalcularExistenciasVigentes(lotesMedicamento, hoy);
                var vigentes = PropuestaFefo.LotesVigentesEnOrden(lotesMedicamento, hoy)
                    .Select(l => new LoteVigenteDto(
                        l.Id, l.NumeroLote, l.FechaCaducidad, l.Stock, l.ObtenerSemaforo(hoy).ToString()))
                    .ToList();

                return new MedicamentoParaSalidaDto(
                    m.Id,
                    m.NombreComercial,
                    m.NombreSustancia,
                    m.Presentacion,
                    m.Concentracion,
                    m.EsControlado,
                    existencias.Vigentes,
                    existencias.EstaAgotado,
                    vigentes);
            })
            .ToList();
    }
}
