using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;

namespace EtbotFarma.Application.SalidaMedicamentos.Queries;

public sealed record ProponerSalidaMedicamentoQuery(int IdMedicamento, string Cantidad)
    : IQuery<PropuestaSalidaDto>;
