using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.MedicamentosAgotados.Dtos;

namespace EtbotFarma.Application.MedicamentosAgotados.Queries;

public sealed record ObtenerMedicamentosAgotadosQuery(string? Termino) : IQuery<IReadOnlyList<MedicamentoAgotadoDto>>;
