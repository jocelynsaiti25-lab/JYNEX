using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;

namespace EtbotFarma.Application.SalidaMedicamentos.Commands;

public sealed record RegistrarSalidaMedicamentoCommand(int IdMedicamento, string Cantidad)
    : ICommand<SalidaMedicamentoRegistradaDto>;
