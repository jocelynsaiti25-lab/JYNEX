namespace EtbotFarma.Api.Historias.EntradaLote;

public sealed record RegistrarEntradaLoteResponse(
    int IdLote,
    int IdMedicamento,
    string NombreMedicamento,
    string NumeroLote,
    string FechaCaducidad,
    int CantidadIngresada,
    string SemaforoLote,
    int ExistenciasTotalesActualizadas,
    string SemaforoGeneralActualizado,
    bool EstaAgotado,
    bool EnStockBajo,
    string Mensaje);