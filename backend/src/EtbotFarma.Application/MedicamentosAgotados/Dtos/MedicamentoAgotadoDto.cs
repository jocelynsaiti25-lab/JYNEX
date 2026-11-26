namespace EtbotFarma.Application.MedicamentosAgotados.Dtos;

/// <summary>
/// HU-12 escenario 3: datos de un medicamento sin existencias vigentes, para la lista de agotados.
/// </summary>
public sealed record MedicamentoAgotadoDto(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion);
