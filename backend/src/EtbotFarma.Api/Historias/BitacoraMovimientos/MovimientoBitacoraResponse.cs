namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

public sealed record MovimientoBitacoraResponse(
    int IdMovimiento,
    DateTime Fecha,
    int IdUsuario,
    string Usuario,
    string Tipo,
    int IdMedicamento,
    string Medicamento,
    string Presentacion,
    string Concentracion,
    int CantidadTotal,
    string? Motivo,
    IReadOnlyList<DetalleBitacoraResponse> Lotes);
