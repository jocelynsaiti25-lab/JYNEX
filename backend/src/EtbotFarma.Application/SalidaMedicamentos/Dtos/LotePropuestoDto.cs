namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

public sealed record LotePropuestoDto(
    int IdLote,
    string NumeroLote,
    DateOnly FechaCaducidad,
    int Cantidad);
