using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;

namespace EtbotFarma.Application.SalidaMedicamentos.Queries;

public sealed record BuscarMedicamentosParaSalidaQuery(string? Termino)
    : IQuery<IReadOnlyList<MedicamentoParaSalidaDto>>;
