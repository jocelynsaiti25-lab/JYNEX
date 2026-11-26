using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using EtbotFarma.Domain.ValueObjects;

namespace EtbotFarma.Application.SalidaMedicamentos.Handlers;

public sealed class ProponerSalidaMedicamentoHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj)
    : IQueryHandler<ProponerSalidaMedicamentoQuery, PropuestaSalidaDto>
{
    public async Task<PropuestaSalidaDto> Handle(
        ProponerSalidaMedicamentoQuery query,
        CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        var medicamento = await medicamentos.ObtenerPorIdAsync(query.IdMedicamento, cancellationToken);
        if (medicamento is null || !medicamento.Activo)
            throw new NoEncontradoException("El medicamento no existe o está inactivo.");

        var lotesMedicamento = await lotes.ObtenerPorMedicamentosAsync([medicamento.Id], cancellationToken);
        var propuesta = PropuestaFefo.Calcular(lotesMedicamento, query.CantidadSolicitada, reloj.Hoy);

        return new PropuestaSalidaDto(
            medicamento.Id,
            medicamento.NombreComercial,
            query.CantidadSolicitada,
            propuesta.CantidadDisponible,
            propuesta.Lotes
                .Select(a => new LoteSalidaDto(
                    a.Lote.Id,
                    a.Lote.NumeroLote,
                    a.Lote.FechaCaducidad,
                    a.Lote.Stock,
                    a.Cantidad))
                .ToList(),
            propuesta.Alcanza);
    }
}
