namespace EtbotFarma.Api.Historias.EdicionMedicamentos;

/// <summary>CONTRATO de salida (JSON) de PUT api/medicamentos/{idMedicamento}.</summary>
public sealed record EditarMedicamentoResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    bool SeGuardaronCambios,
    string Mensaje);
