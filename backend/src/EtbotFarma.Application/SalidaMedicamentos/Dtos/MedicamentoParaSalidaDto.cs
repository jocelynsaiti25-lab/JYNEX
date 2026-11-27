namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

public sealed record MedicamentoParaSalidaDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int ExistenciasVigentes,
    bool EstaAgotado,
    IReadOnlyList<LoteVigenteDto> Lotes);
