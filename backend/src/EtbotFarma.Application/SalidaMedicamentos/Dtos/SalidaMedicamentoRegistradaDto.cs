namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

public sealed record SalidaMedicamentoRegistradaDto(
    int IdMovimiento,
    int IdMedicamento,
    string Medicamento,
    int CantidadTotal,
    IReadOnlyList<LotePropuestoDto> Lotes,
    string Mensaje);
