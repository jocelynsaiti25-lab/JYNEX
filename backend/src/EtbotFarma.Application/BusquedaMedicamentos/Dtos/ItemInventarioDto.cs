namespace EtbotFarma.Application.BusquedaMedicamentos.Dtos;

/// <summary>
/// HU-08 escenario 3 y HU-10: datos completos de cada fila de medicamento y lote en el inventario,
/// incluyendo su clasificación de semáforo y si ya está caducado (HU-10 escenario 5).
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
    bool EstaCaducado,
    string EstadoDisponibilidad);