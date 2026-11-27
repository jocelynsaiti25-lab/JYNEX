using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class MovimientoRepositoryFalso : IMovimientoRepository
{
    public List<Movimiento> Movimientos { get; } = [];

    public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
    {
        Movimientos.Add(movimiento);
        return Task.FromResult(Movimientos.Count);
    }
}
