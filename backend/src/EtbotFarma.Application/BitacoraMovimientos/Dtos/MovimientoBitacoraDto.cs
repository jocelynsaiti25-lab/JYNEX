namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

public sealed record MovimientoBitacoraDto(
    int IdMovimiento,
    DateTime FechaUtc,
    int IdUsuario,
    string Usuario,
    string Tipo,
    int IdMedicamento,
    string Medicamento,
    string Presentacion,
    string Concentracion,
    int CantidadTotal,
    string? Motivo,
    IReadOnlyList<DetalleBitacoraDto> Lotes);
