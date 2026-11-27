namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

public sealed record PropuestaSalidaDto(
    int IdMedicamento,
    string Medicamento,
    int CantidadTotal,
    int ExistenciasVigentes,
    IReadOnlyList<LotePropuestoDto> Lotes);
