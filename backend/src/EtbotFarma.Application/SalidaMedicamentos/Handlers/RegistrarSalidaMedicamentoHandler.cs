using System.Globalization;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Domain.ValueObjects;

namespace EtbotFarma.Application.SalidaMedicamentos.Handlers;

public sealed class RegistrarSalidaMedicamentoHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IMovimientoRepository movimientos,
    IUsuarioActual usuarioActual,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<RegistrarSalidaMedicamentoCommand, SalidaMedicamentoRegistradaDto>
{
    public async Task<SalidaMedicamentoRegistradaDto> Handle(
        RegistrarSalidaMedicamentoCommand command,
        CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is not int idUsuario)
            throw new NoAutenticadoException();

        var medicamento = await medicamentos.ObtenerPorIdAsync(command.IdMedicamento, cancellationToken)
            ?? throw new NoEncontradoException("El medicamento no existe");

        if (!medicamento.Activo)
            throw new DomainException("No se puede registrar una salida de un medicamento inactivo.");

        var cantidad = int.Parse(command.Cantidad.Trim(), CultureInfo.InvariantCulture);
        var lotesBloqueados = await lotes.ObtenerPorMedicamentoParaActualizarAsync(medicamento.Id, cancellationToken);
        var propuesta = PropuestaFefo.Calcular(lotesBloqueados, cantidad, reloj.Hoy);

        foreach (var asignacion in propuesta.Asignaciones)
        {
            var descontado = await lotes.DescontarStockAsync(asignacion.IdLote, asignacion.Cantidad, cancellationToken);
            if (!descontado)
                throw new DomainException("Stock insuficiente: las existencias cambiaron y ya no alcanzan para la salida.");
        }

        var movimiento = Movimiento.CrearSalida(
            medicamento.Id,
            idUsuario,
            propuesta.Asignaciones,
            reloj.UtcNow.UtcDateTime);
        var idMovimiento = await movimientos.AgregarConDetallesAsync(movimiento, cancellationToken);

        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new SalidaMedicamentoRegistradaDto(
            idMovimiento,
            medicamento.Id,
            $"{medicamento.NombreComercial} {medicamento.Concentracion} {medicamento.Presentacion}",
            propuesta.CantidadTotal,
            propuesta.Asignaciones
                .Select(a => new LotePropuestoDto(a.IdLote, a.NumeroLote, a.FechaCaducidad, a.Cantidad))
                .ToList(),
            "Salida registrada");
    }
}
