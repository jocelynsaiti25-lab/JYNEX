namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

/// <summary>Contrato HTTP del movimiento y todos sus lotes asociados.</summary>
public sealed record MovimientoBitacoraResponse(
    int IdMovimiento,
    DateTime Fecha,
    string Usuario,
    string Tipo,
    int IdMedicamento,
    string Medicamento,
    int CantidadTotal,
    string? Motivo,
    IReadOnlyList<DetalleBitacoraResponse> Detalles);
