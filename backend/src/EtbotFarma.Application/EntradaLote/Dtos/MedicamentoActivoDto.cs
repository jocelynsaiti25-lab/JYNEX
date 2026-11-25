namespace EtbotFarma.Application.EntradaLote.Dtos;

/// <summary>Datos descriptivos y estado de stock de un medicamento activo para selección en HU-07.</summary>
public sealed record MedicamentoActivoDto(
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