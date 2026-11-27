using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Queries;

public sealed record ConsultarMedicamentosBitacoraQuery : IQuery<IReadOnlyList<MedicamentoBitacoraDto>>;
