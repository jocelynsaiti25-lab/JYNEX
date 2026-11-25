using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.RegistroMedicamentos.Dtos;

namespace EtbotFarma.Application.RegistroMedicamentos.Commands;

/// <summary>
/// HU-06: datos de entrada para dar de alta un medicamento nuevo con su lote inicial.
/// </summary>
public sealed record RegistrarMedicamentoCommand(
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado,
    string NumeroLote,
    string FechaCaducidad,
    string CantidadInicial) : ICommand<MedicamentoRegistradoDto>;

