namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

/// <summary>Contrato HTTP del lote asociado a un movimiento.</summary>
public sealed record DetalleBitacoraResponse(int IdLote, string NumeroLote, int Cantidad);
