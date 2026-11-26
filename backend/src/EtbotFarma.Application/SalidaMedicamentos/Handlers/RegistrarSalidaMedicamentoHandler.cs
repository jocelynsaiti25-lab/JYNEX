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
    IDateTimeProvider reloj)
    : ICommandHandler<RegistrarSalidaMedicamentoCommand, SalidaMedicamentoRegistradoDto>
{
    public async Task<SalidaMedicamentoRegistradoDto> Handle(
        RegistrarSalidaMedicamentoCommand command,
        CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is not int idUsuario)
            throw new NoAutenticadoException();

        var medicamento = await medicamentos.ObtenerPorIdAsync(command.IdMedicamento, cancellationToken);
        if (medicamento is null || !medicamento.Activo)
            throw new NoEncontradoException("El medicamento no existe o está inactivo.");

        var hoy = reloj.Hoy;
        var lotesVigentes = await lotes.ObtenerVigentesParaSalidaAsync(medicamento.Id, hoy, cancellationToken);
        var propuesta = PropuestaFefo.Calcular(lotesVigentes, command.Cantidad, hoy);

        if (!propuesta.Alcanza)
        {
            throw new DomainException(
                $"Existencias insuficientes. Disponibles: {propuesta.CantidadDisponible}. Solicitadas: {command.Cantidad}.");
        }

        foreach (var asignado in propuesta.Lotes)
        {
            if (!await lotes.DescontarStockAsync(asignado.Lote.Id, asignado.Cantidad, cancellationToken))
            {
                throw new DomainException(
                    "Las existencias del lote cambiaron. La salida no se registró; vuelve a consultar la propuesta.");
            }
        }

        var movimiento = Movimiento.CrearSalida(
            medicamento.Id,
            idUsuario,
            propuesta.Lotes.Select(a => (a.Lote.Id, a.Cantidad)).ToList(),
            reloj.UtcNow.UtcDateTime);

        var idMovimiento = await movimientos.AgregarConDetallesAsync(movimiento, cancellationToken);

        var lotesActualizados = await lotes.ObtenerPorMedicamentosAsync([medicamento.Id], cancellationToken);
        var existencias = medicamento.CalcularExistencias(lotesActualizados, hoy);

        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new SalidaMedicamentoRegistradoDto(
            idMovimiento,
            medicamento.Id,
            medicamento.NombreComercial,
            command.Cantidad,
            propuesta.Lotes
                .Select(a => new LoteSalidaDto(
                    a.Lote.Id,
                    a.Lote.NumeroLote,
                    a.Lote.FechaCaducidad,
                    a.Lote.Stock,
                    a.Cantidad))
                .ToList(),
            existencias.Total,
            existencias.SemaforoGeneral,
            existencias.EstaAgotado,
            existencias.EnStockBajo,
            "Salida de medicamentos registrada correctamente.");
    }
}
