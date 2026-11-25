namespace EtbotFarma.Application.Inventario.Dtos;

/// <summary>
/// HU-08 escenario 3: datos completos de cada fila de medicamento y lote en el inventario.
/// </summary>
public sealed record ItemInventarioDto(
    int IdMedicamento,
    int? IdLote,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int StockMinimo,
    string NumeroLote,
    int Existencias,
    DateOnly? FechaCaducidad,
    string SemaforoCaducidad,
    string EstadoDisponibilidad);