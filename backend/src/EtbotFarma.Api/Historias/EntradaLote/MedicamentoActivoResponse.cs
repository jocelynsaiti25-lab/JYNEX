namespace EtbotFarma.Api.Historias.EntradaLote;

public sealed record MedicamentoActivoResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int StockMinimo,
    int ExistenciasTotales,
    string SemaforoGeneral,
    bool EstaAgotado,
    bool EnStockBajo);