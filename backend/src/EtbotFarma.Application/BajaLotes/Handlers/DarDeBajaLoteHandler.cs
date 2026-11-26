using EtbotFarma.Application.BajaLotes.Commands;
using EtbotFarma.Application.BajaLotes.Dtos;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.BajaLotes.Handlers;

public sealed class DarDeBajaLoteHandler(
    ILoteRepository lotes,
    IMedicamentoRepository medicamentos,
    IMovimientoRepository movimientos,
    IUsuarioActual usuarioActual,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<DarDeBajaLoteCommand, LoteDadoDeBajaDto>
{
    public async Task<LoteDadoDeBajaDto> Handle(DarDeBajaLoteCommand command, CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is not int idUsuario)
            throw new NoAutenticadoException();

        var lote = await lotes.ObtenerPorIdParaActualizarAsync(command.IdLote, cancellationToken)
            ?? throw new NoEncontradoException("El lote no se encontró.");

        var medicamento = await medicamentos.ObtenerPorIdAsync(lote.IdMedicamento, cancellationToken)
            ?? throw new NoEncontradoException("El lote no se encontró.");

        var cantidadRetirada = lote.DarDeBaja(reloj.Hoy, command.CantidadConsultada);

        var movimiento = Movimiento.CrearBaja(
            medicamento.Id,
            lote.Id,
            idUsuario,
            cantidadRetirada,
            command.Motivo,
            reloj.UtcNow.UtcDateTime);

        if (!await lotes.DescontarStockAsync(lote.Id, cantidadRetirada, cancellationToken))
            throw new DomainException(Lote.MensajeNoElegible);

        await movimientos.AgregarConDetallesAsync(movimiento, cancellationToken);

        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new LoteDadoDeBajaDto(
            lote.Id,
            medicamento.Id,
            $"{medicamento.NombreComercial} {medicamento.Concentracion} {medicamento.Presentacion}",
            lote.NumeroLote,
            lote.FechaCaducidad,
            cantidadRetirada,
            movimiento.Motivo!,
            "Lote dado de baja");
    }
}
