namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

public sealed record MedicamentoBitacoraDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion);
