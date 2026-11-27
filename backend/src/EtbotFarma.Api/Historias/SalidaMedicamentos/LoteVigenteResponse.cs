namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record LoteVigenteResponse(
    int IdLote,
    string NumeroLote,
    string FechaCaducidad,
    int Existencias,
    string Semaforo);
