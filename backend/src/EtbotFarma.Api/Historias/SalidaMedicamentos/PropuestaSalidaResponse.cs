namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record PropuestaSalidaResponse(
    int IdMedicamento,
    string Medicamento,
    int CantidadTotal,
    int ExistenciasVigentes,
    IReadOnlyList<LotePropuestoResponse> Lotes);
