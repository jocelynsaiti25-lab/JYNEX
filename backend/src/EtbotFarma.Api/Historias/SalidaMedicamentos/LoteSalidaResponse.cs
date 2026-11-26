
namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record LoteSalidaResponse(
    int IdLote,
    string NumeroLote,
    string FechaCaducidad,
    int StockDisponible,
    int CantidadAUtilizar);
