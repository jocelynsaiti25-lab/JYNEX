namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

/// <summary>Encabezado de movimiento con todos sus detalles y datos descriptivos actuales.</summary>
public sealed record MovimientoBitacoraDto(
    int IdMovimiento,
    DateTime Fecha,
    string Usuario,
    string Tipo,
    int IdMedicamento,
    string Medicamento,
    int CantidadTotal,
    string? Motivo,
    IReadOnlyList<DetalleBitacoraDto> Detalles);
