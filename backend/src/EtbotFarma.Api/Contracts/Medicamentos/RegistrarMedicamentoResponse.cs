namespace EtbotFarma.Api.Contracts.Medicamentos;

/// <summary>
/// Contrato JSON que devuelve POST /api/medicamentos tras registrar el medicamento y su lote inicial.
/// </summary>
public sealed record RegistrarMedicamentoResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int IdLote,
    string NumeroLote,
    string FechaCaducidad,
    int Stock,
    string SemaforoCaducidad,
    bool SeRegistroEntrada,
    string Mensaje);