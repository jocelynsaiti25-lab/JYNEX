namespace EtbotFarma.Application.EdicionMedicamentos.Dtos;

/// <summary>HU-09 escenarios 1, 9 y 12: resultado de guardar (o no) la edición.</summary>
public sealed record MedicamentoEditadoDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    bool SeGuardaronCambios,
    string Mensaje);
