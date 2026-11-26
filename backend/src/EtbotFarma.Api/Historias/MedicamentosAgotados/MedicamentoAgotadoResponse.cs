namespace EtbotFarma.Api.Historias.MedicamentosAgotados;

public sealed record MedicamentoAgotadoResponse(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion);
