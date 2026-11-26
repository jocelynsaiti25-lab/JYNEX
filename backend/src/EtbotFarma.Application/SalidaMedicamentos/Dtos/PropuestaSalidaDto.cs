
namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

/// <summary>
/// Información de la propuesta de salida de medicamentos.
/// </summary>
public sealed record PropuestaSalidaDto(
    int IdMedicamento,
    string NombreMedicamento,
    int CantidadSolicitada,
    int CantidadDisponible,
    IReadOnlyList<LoteSalidaDto> Lotes,
    bool PuedeRealizarse);
