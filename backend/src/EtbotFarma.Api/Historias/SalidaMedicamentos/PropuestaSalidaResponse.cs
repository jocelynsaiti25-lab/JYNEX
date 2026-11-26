
namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record PropuestaSalidaResponse(
    int IdMedicamento,
    string NombreMedicamento,
    int CantidadSolicitada,
    int CantidadDisponible,
    IReadOnlyList<LoteSalidaResponse> Lotes,
    bool PuedeRealizarse);
