namespace EtbotFarma.Api.Historias.StockMinimo;

public sealed record MedicamentoStockResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    int StockMinimo,
    int ExistenciasVigentes,
    int ExistenciasCaducadas,
    bool TieneLotesCaducados,
    bool EnStockBajo,
    bool EstaAgotado);
