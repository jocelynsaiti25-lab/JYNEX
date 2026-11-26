

using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;
using EtbotFarma.Application.SalidaMedicamentos.Queries;

namespace EtbotFarma.Application.SalidaMedicamentos.Handlers;

/// <summary>
/// HU-13: propone los lotes que se utilizarán para realizar una salida,
/// respetando el criterio FEFO.
/// </summary>
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

        if (query.CantidadSolicitada <= 0)
        {
            throw new ValidacionException(
                ["La cantidad solicitada debe ser mayor que cero."]);
        }

        var medicamento = await medicamentos.ObtenerPorIdAsync(
            query.IdMedicamento,
            cancellationToken);

        if (medicamento is null || !medicamento.Activo)
            throw new NoEncontradoException(
                "El medicamento no existe o está inactivo.");

        var hoy = reloj.Hoy;

        var lotesMedicamento = await lotes.ObtenerPorMedicamentosAsync(
            new[] { medicamento.Id },
            cancellationToken);

        var lotesVigentes = lotesMedicamento
            .Where(l => l.Stock > 0 && !l.EstaCaducado(hoy))
            .OrderBy(l => l.FechaCaducidad)
            .ThenBy(l => l.Id)
            .ToList();

        var cantidadDisponible = lotesVigentes.Sum(l => l.Stock);
        var cantidadRestante = query.CantidadSolicitada;

        var propuesta = new List<LoteSalidaDto>();

        foreach (var lote in lotesVigentes)
        {
            if (cantidadRestante <= 0)
                break;

            var cantidadAUtilizar = Math.Min(cantidadRestante, lote.Stock);

            propuesta.Add(new LoteSalidaDto(
                lote.Id,
                lote.NumeroLote,
                lote.FechaCaducidad,
                lote.Stock,
                cantidadAUtilizar));

            cantidadRestante -= cantidadAUtilizar;
        }

        return new PropuestaSalidaDto(
            medicamento.Id,
            medicamento.NombreComercial,
            query.CantidadSolicitada,
            cantidadDisponible,
            propuesta,
            cantidadDisponible >= query.CantidadSolicitada);
    }
}
