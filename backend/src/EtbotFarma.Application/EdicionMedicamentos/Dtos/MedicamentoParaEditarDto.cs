namespace EtbotFarma.Application.EdicionMedicamentos.Dtos;

/// <summary>
/// HU-09 escenario 2: datos para precargar el formulario de edición.
/// ExistenciasTotales y SemaforoCaducidad son solo de REFERENCIA (no editables aquí: escenario 10).
/// </summary>
public sealed record MedicamentoParaEditarDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int ExistenciasTotales,
    string SemaforoCaducidad);
