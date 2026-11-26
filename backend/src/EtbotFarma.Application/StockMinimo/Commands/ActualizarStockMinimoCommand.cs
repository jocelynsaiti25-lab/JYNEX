using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.StockMinimo.Dtos;

namespace EtbotFarma.Application.StockMinimo.Commands;

/// <summary>HU-11 escenarios 1 y 6: define o edita el stock mínimo de un medicamento.</summary>
public sealed record ActualizarStockMinimoCommand(
    int IdMedicamento,
    int? NuevoStockMinimo) : ICommand<StockMinimoActualizadoDto>;
