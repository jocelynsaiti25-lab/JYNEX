namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

/// <summary>Lote y cantidad asociados a un movimiento de inventario.</summary>
public sealed record DetalleBitacoraDto(int IdLote, string NumeroLote, int Cantidad);
