namespace EtbotFarma.Api.Historias.BusquedaMedicamentos;

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
    bool EstaCaducado,
    string EstadoDisponibilidad);