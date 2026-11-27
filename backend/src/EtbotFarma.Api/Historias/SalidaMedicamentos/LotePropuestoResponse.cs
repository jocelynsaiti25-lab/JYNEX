namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record LotePropuestoResponse(
    int IdLote,
    string NumeroLote,
    string FechaCaducidad,
    int Cantidad);
