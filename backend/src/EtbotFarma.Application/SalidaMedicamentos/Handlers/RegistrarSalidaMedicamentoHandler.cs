
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.SalidaMedicamentos.Handlers;

/// <summary>
/// HU-13: registra la salida de medicamentos, descuenta los lotes
/// siguiendo FEFO y registra el movimiento en una única transacción.
/// </summary>
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
        // 1. Verificar sesión activa.
        if (usuarioActual.IdUsuario is not int idUsuario)
            throw new NoAutenticadoException();

        // 2. Validar cantidad solicitada.
        if (command.Cantidad <= 0)
        {
            throw new ValidacionException(
                ["La cantidad solicitada debe ser mayor que cero."]);
        }

        // 3. Verificar que el medicamento exista y esté activo.
        var medicamento = await medicamentos.ObtenerPorIdAsync(
            command.IdMedicamento,
            cancellationToken);

        if (medicamento is null || !medicamento.Activo)
            throw new NoEncontradoException(
                "El medicamento no existe o está inactivo.");

        var hoy = reloj.Hoy;
        var ahoraUtc = reloj.UtcNow.UtcDateTime;

        // 4. Obtener lotes vigentes bloqueados dentro de la transacción.
        var lotesVigentes = await lotes.ObtenerVigentesParaSalidaAsync(
            medicamento.Id,
            hoy,
            cancellationToken);

        var cantidadDisponible = lotesVigentes.Sum(l => l.Stock);

        // 5. Verificar existencias suficientes antes de descontar.
        if (cantidadDisponible < command.Cantidad)
        {
            throw new DomainException(
                $"Existencias insuficientes. Disponibles: {cantidadDisponible}. Solicitadas: {command.Cantidad}.");
        }

        // 6. Distribuir la cantidad solicitada siguiendo FEFO.
        var cantidadRestante = command.Cantidad;

        var lotesUtilizados = new List<LoteSalidaDto>();
        var lotesSeleccionados = new List<(int IdLote, int Cantidad)>();

        foreach (var lote in lotesVigentes)
        {
            if (cantidadRestante <= 0)
                break;

            var cantidadAUtilizar = Math.Min(
                cantidadRestante,
                lote.Stock);

            lotesUtilizados.Add(new LoteSalidaDto(
                lote.Id,
                lote.NumeroLote,
                lote.FechaCaducidad,
                lote.Stock,
                cantidadAUtilizar));

            lotesSeleccionados.Add((
                lote.Id,
                cantidadAUtilizar));

            cantidadRestante -= cantidadAUtilizar;
        }

        // 7. Descontar existencias sin permitir stock negativo.
        foreach (var lote in lotesSeleccionados)
        {
            var descontado = await lotes.DescontarStockAsync(
                lote.IdLote,
                lote.Cantidad,
                cancellationToken);

            if (!descontado)
            {
                throw new DomainException(
                    "No fue posible descontar las existencias del lote. La operación fue cancelada.");
            }
        }

        // 8. Crear y registrar el movimiento SALIDA con sus detalles.
        var movimiento = Movimiento.CrearSalida(
            medicamento.Id,
            idUsuario,
            lotesSeleccionados,
            ahoraUtc);

        var idMovimiento = await movimientos.AgregarConDetallesAsync(
            movimiento,
            cancellationToken);

        // 9. Recalcular existencias y semáforo.
        var lotesActualizados = await lotes.ObtenerPorMedicamentosAsync(
            [medicamento.Id],
            cancellationToken);

        var existencias = medicamento.CalcularExistencias(
            lotesActualizados,
            hoy);

        // 10. Confirmar toda la transacción.
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new SalidaMedicamentoRegistradoDto(
            idMovimiento,
            medicamento.Id,
            medicamento.NombreComercial,
            command.Cantidad,
            lotesUtilizados,
            existencias.Total,
            existencias.SemaforoGeneral,
            existencias.EstaAgotado,
            existencias.EnStockBajo,
            "Salida de medicamentos registrada correctamente.");
    }
}
