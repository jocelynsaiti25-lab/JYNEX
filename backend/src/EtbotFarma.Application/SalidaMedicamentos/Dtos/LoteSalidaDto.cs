
namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

/// <summary>
/// Información de un lote seleccionado para la salida de medicamentos.
/// </summary>
public sealed record LoteSalidaDto(
    int IdLote,
    string NumeroLote,
    DateOnly FechaCaducidad,
    int StockDisponible,
    int CantidadAUtilizar);
