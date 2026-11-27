namespace EtbotFarma.Application.RegistroMedicamentos.Dtos;

/// <summary>
/// Resultado del registro exitoso de un medicamento y su lote inicial (HU-06 escenarios 1, 2, 11 y 20).
/// </summary>
public sealed record MedicamentoRegistradoDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    int IdLote,
    string NumeroLote,
    DateOnly FechaCaducidad,
    int Stock,
    string SemaforoCaducidad,
    bool SeRegistroEntrada,
    string Mensaje);

