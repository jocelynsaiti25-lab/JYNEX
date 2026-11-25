namespace EtbotFarma.Api.Historias.EntradaLote;

public sealed record RegistrarEntradaLoteRequest(
    int? IdMedicamento,
    string? NumeroLote,
    string? FechaCaducidad,
    string? Cantidad);