namespace EtbotFarma.Api.Historias.EdicionMedicamentos;

/// <summary>
/// CONTRATO de salida (JSON) de GET api/medicamentos/{idMedicamento}: datos para precargar el formulario
/// de edición. ExistenciasTotales y SemaforoCaducidad son solo de referencia (escenario 10).
/// </summary>
public sealed record MedicamentoParaEditarResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int ExistenciasTotales,
    string SemaforoCaducidad);
