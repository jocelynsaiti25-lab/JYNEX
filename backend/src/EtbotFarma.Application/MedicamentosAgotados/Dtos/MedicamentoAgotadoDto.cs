namespace EtbotFarma.Application.MedicamentosAgotados.Dtos;

public sealed record MedicamentoAgotadoDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    string Etiqueta)
{
    public const string EtiquetaSinExistencias = "SIN EXISTENCIAS";
}
