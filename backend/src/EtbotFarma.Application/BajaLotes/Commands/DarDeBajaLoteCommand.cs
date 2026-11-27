using EtbotFarma.Application.BajaLotes.Dtos;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BajaLotes.Commands;

public sealed record DarDeBajaLoteCommand(
    int IdLote,
    int CantidadConsultada,
    string Motivo) : ICommand<LoteDadoDeBajaDto>;
