namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

public sealed record MedicamentoBitacoraResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion);
