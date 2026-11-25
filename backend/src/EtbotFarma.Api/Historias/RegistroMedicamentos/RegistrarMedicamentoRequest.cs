namespace EtbotFarma.Api.Historias.RegistroMedicamentos;

/// <summary>
/// Contrato JSON que recibe POST /api/medicamentos (HU-06).
/// Los campos son nullable para que RegistrarMedicamentoValidator devuelva mensajes claros en español.
/// </summary>
public sealed record RegistrarMedicamentoRequest(
    string? NombreComercial,
    string? NombreSustancia,
    string? Presentacion,
    string? Concentracion,
    bool EsControlado,
    string? NumeroLote,
    string? FechaCaducidad,
    string? CantidadInicial);