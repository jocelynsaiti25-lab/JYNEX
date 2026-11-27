using System.Globalization;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.ValueObjects;

namespace EtbotFarma.Application.SalidaMedicamentos.Handlers;

public sealed class ProponerSalidaMedicamentoHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj) : IQueryHandler<ProponerSalidaMedicamentoQuery, PropuestaSalidaDto>
{
    public async Task<PropuestaSalidaDto> Handle(
        ProponerSalidaMedicamentoQuery query,
        CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        var medicamento = await medicamentos.ObtenerPorIdAsync(query.IdMedicamento, cancellationToken)
            ?? throw new NoEncontradoException("El medicamento no existe");

        if (!medicamento.Activo)
            throw new DomainException("No se puede registrar una salida de un medicamento inactivo.");

        var cantidad = int.Parse(query.Cantidad.Trim(), CultureInfo.InvariantCulture);
        var lotesMedicamento = await lotes.ObtenerPorMedicamentosAsync([medicamento.Id], cancellationToken);
        var propuesta = PropuestaFefo.Calcular(lotesMedicamento, cantidad, reloj.Hoy);

        return new PropuestaSalidaDto(
            medicamento.Id,
            $"{medicamento.NombreComercial} {medicamento.Concentracion} {medicamento.Presentacion}",
            propuesta.CantidadTotal,
            propuesta.ExistenciasVigentes,
            propuesta.Asignaciones
                .Select(a => new LotePropuestoDto(a.IdLote, a.NumeroLote, a.FechaCaducidad, a.Cantidad))
                .ToList());
    }
}
