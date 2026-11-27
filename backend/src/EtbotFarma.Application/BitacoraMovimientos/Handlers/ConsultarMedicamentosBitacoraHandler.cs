using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Handlers;

public sealed class ConsultarMedicamentosBitacoraHandler(IMovimientoRepository movimientos)
    : IQueryHandler<ConsultarMedicamentosBitacoraQuery, IReadOnlyList<MedicamentoBitacoraDto>>
{
    public Task<IReadOnlyList<MedicamentoBitacoraDto>> Handle(
        ConsultarMedicamentosBitacoraQuery query,
        CancellationToken cancellationToken) =>
        movimientos.ListarMedicamentosAsync(cancellationToken);
}
