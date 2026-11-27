namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

public sealed record LoteVigenteDto(
    int IdLote,
    string NumeroLote,
    DateOnly FechaCaducidad,
    int Existencias,
    string Semaforo);
