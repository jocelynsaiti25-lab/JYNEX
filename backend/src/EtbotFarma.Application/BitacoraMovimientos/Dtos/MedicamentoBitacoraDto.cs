namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

/// <summary>Opción de filtro asociada a medicamentos con movimientos, activos o inactivos.</summary>
public sealed record MedicamentoBitacoraDto(int IdMedicamento, string NombreComercial, string NombreSustancia);
