namespace EtbotFarma.Application.StockMinimo.Dtos;

/// <summary>
/// HU-11 escenarios 1 al 4: datos de un medicamento para la pantalla de control de stock mínimo,
/// con sus existencias vigentes (no caducadas) y el estado de alerta ya calculado.
/// </summary>
public sealed record MedicamentoStockDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    int StockMinimo,
    int ExistenciasVigentes,
    bool EnStockBajo,
    bool EstaAgotado);
