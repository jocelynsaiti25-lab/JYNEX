namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record RegistrarSalidaMedicamentoResponse(
    int IdMovimiento,
    int IdMedicamento,
    string Medicamento,
    int CantidadTotal,
    IReadOnlyList<LotePropuestoResponse> Lotes,
    string Mensaje);
