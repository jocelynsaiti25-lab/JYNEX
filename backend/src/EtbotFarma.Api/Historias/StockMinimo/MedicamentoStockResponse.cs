namespace EtbotFarma.Api.Historias.StockMinimo;

public sealed record MedicamentoStockResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    int StockMinimo,
    int ExistenciasVigentes,
    bool EnStockBajo,
    bool EstaAgotado);
