using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EdicionMedicamentos.Dtos;

namespace EtbotFarma.Application.EdicionMedicamentos.Commands;

/// <summary>
/// HU-09: datos descriptivos editados de un medicamento ya registrado.
/// A propósito NO tiene lote, stock ni fecha de caducidad (escenario 10: no se editan desde aquí).
/// </summary>
public sealed record EditarMedicamentoCommand(
    int IdMedicamento,
    string NombreComercial,
    string NombreSustancia,
    string Presentacion,
    string Concentracion,
    bool EsControlado) : ICommand<MedicamentoEditadoDto>;
