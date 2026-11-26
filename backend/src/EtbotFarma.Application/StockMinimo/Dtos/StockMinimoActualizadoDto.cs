namespace EtbotFarma.Application.StockMinimo.Dtos;

/// <summary>HU-11 escenario 6: resultado de guardar el nuevo stock mínimo.</summary>
public sealed record StockMinimoActualizadoDto(
    int IdMedicamento,
    int StockMinimo,
    string Mensaje);
