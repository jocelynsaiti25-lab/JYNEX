namespace EtbotFarma.Api.Historias.Inventario;

public sealed record ItemInventarioResponse(
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
    string? FechaCaducidad,
    string SemaforoCaducidad,
    string EstadoDisponibilidad);