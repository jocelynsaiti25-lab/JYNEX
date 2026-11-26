using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.MedicamentosAgotados.Dtos;

namespace EtbotFarma.Application.MedicamentosAgotados.Queries;

/// <summary>
/// HU-12: lista los medicamentos activos sin existencias vigentes (agotados), independiente
/// del filtro de semáforo de caducidad. Solo LEE.
/// </summary>
public sealed record ObtenerMedicamentosAgotadosQuery : IQuery<IReadOnlyList<MedicamentoAgotadoDto>>;
