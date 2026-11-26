using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.StockMinimo.Commands;
using EtbotFarma.Application.StockMinimo.Dtos;

namespace EtbotFarma.Application.StockMinimo.Handlers;

/// <summary>
/// HU-11 escenarios 1 y 6: guarda el nuevo umbral de stock mínimo de un medicamento.
/// Disponible para cualquier usuario autenticado (Administrador o Empleado).
/// Los datos YA llegan validados: MediatR ejecutó ActualizarStockMinimoValidator antes.
/// </summary>
public sealed class ActualizarStockMinimoHandler(
    IMedicamentoRepository medicamentos,
    IUnitOfWork unitOfWork) : ICommandHandler<ActualizarStockMinimoCommand, StockMinimoActualizadoDto>
{
    public async Task<StockMinimoActualizadoDto> Handle(
        ActualizarStockMinimoCommand command,
        CancellationToken cancellationToken)
    {
        // 1. El medicamento debe existir
        var medicamento = await medicamentos.ObtenerPorIdAsync(command.IdMedicamento, cancellationToken)
            ?? throw new NoEncontradoException("El medicamento no existe");

        // 2. Aplicar la regla de negocio (Domain) y guardar
        medicamento.ActualizarStockMinimo(command.NuevoStockMinimo!.Value);
        await medicamentos.ActualizarStockMinimoAsync(medicamento.Id, medicamento.StockMinimo, cancellationToken);

        // 3. COMMIT
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new StockMinimoActualizadoDto(medicamento.Id, medicamento.StockMinimo, "Stock mínimo actualizado");
    }
}
