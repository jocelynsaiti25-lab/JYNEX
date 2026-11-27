namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record MedicamentoParaSalidaResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int ExistenciasVigentes,
    bool EstaAgotado,
    IReadOnlyList<LoteVigenteResponse> Lotes);
