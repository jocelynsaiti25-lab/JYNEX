using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Queries;

/// <summary>Obtiene medicamentos con historial para los filtros de la bitácora.</summary>
public sealed record ConsultarMedicamentosBitacoraQuery : IQuery<IReadOnlyList<MedicamentoBitacoraDto>>;
