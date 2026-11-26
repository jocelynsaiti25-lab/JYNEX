using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class LoteRepositoryFalso : ILoteRepository
{
    public List<Lote> Lotes { get; } = [];
    public List<(int IdLote, int Cantidad)> Descuentos { get; } = [];
    public bool SimularDescuentoSinFilas { get; init; }

    public Task<int> AgregarAsync(Lote lote, CancellationToken cancellationToken)
    {
        var id = Lotes.Count + 1;
        lote.AsignarId(id);
        Lotes.Add(lote);
        return Task.FromResult(id);
    }

    public Task<Lote?> ObtenerPorIdParaActualizarAsync(int idLote, CancellationToken cancellationToken)
        => Task.FromResult(Lotes.FirstOrDefault(l => l.Id == idLote));

    public Task<bool> ExisteNumeroLoteEnMedicamentoAsync(
        int idMedicamento, string numeroLote, CancellationToken cancellationToken)
        => Task.FromResult(Lotes.Any(l =>
            l.IdMedicamento == idMedicamento
            && l.NumeroLote.Equals(numeroLote.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Lote>> ObtenerPorMedicamentosAsync(
        IReadOnlyCollection<int> idsMedicamento, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Lote>>(Lotes.Where(l => idsMedicamento.Contains(l.IdMedicamento)).ToList());

    public Task<IReadOnlyList<Lote>> ObtenerTodosOrdenadosPorCaducidadAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Lote>>(Lotes.OrderBy(l => l.FechaCaducidad).ToList());

    public Task<bool> DescontarStockAsync(int idLote, int cantidad, CancellationToken cancellationToken)
    {
        if (SimularDescuentoSinFilas)
            return Task.FromResult(false);

        Descuentos.Add((idLote, cantidad));
        return Task.FromResult(true);
    }
}
