
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;

namespace EtbotFarma.Application.SalidaMedicamentos.Commands;

/// <summary>
/// HU-13: comando para registrar la salida de medicamentos.
/// </summary>
public sealed record RegistrarSalidaMedicamentoCommand(
    int IdMedicamento,
    int Cantidad)
    : ICommand<SalidaMedicamentoRegistradoDto>;
