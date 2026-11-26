namespace EtbotFarma.Api.Historias.StockMinimo;

/// <summary>CONTRATO de entrada (JSON) de PUT api/medicamentos/{idMedicamento}/stock-minimo.</summary>
public sealed record ActualizarStockMinimoRequest(int? NuevoStockMinimo);
