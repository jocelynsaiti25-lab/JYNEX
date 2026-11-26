namespace EtbotFarma.Api.Historias.EdicionMedicamentos;

/// <summary>
/// CONTRATO de entrada (JSON) de PUT api/medicamentos/{idMedicamento}.
/// A propósito no tiene lote, stock ni fecha de caducidad: el servidor los ignora (escenario 10).
/// </summary>
public sealed record EditarMedicamentoRequest(
    string? NombreComercial,
    string? NombreSustancia,
    string? Presentacion,
    string? Concentracion,
    bool EsControlado);
