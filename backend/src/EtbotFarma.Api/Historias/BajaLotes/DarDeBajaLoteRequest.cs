namespace EtbotFarma.Api.Historias.BajaLotes;

public sealed record DarDeBajaLoteRequest(
    int? CantidadConsultada,
    string? Motivo);
