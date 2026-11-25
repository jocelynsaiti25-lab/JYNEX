namespace EtbotFarma.Application.EntradaLote.Dtos;

/// <summary>
/// HU-07 escenarios 10, 11 y 12: resultado tras confirmar la entrada del lote y actualizar indicadores.
/// </summary>
public sealed record EntradaLoteRegistradaDto(
    int IdLote,
    int IdMedicamento,
    string NombreMedicamento,
    string NumeroLote,
    DateOnly FechaCaducidad,
    int CantidadIngresada,
    string SemaforoLote,
    int ExistenciasTotalesActualizadas,
    string SemaforoGeneralActualizado,
    bool EstaAgotado,
    bool EnStockBajo,
    string Mensaje);