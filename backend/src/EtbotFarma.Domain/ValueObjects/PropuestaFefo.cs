using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.ValueObjects;

public sealed record LoteAsignado(Lote Lote, int Cantidad);

public sealed record PropuestaFefo(
    int CantidadSolicitada,
    int CantidadDisponible,
    IReadOnlyList<LoteAsignado> Lotes)
{
    public bool Alcanza => CantidadDisponible >= CantidadSolicitada;

    public static PropuestaFefo Calcular(IEnumerable<Lote> lotes, int cantidadSolicitada, DateOnly fechaHoy)
    {
        if (cantidadSolicitada <= 0)
            throw new DomainException("La cantidad debe ser un número entero mayor que cero.");

        var vigentes = lotes
            .Where(l => l.Stock > 0 && !l.EstaCaducado(fechaHoy))
            .OrderBy(l => l.FechaCaducidad)
            .ThenBy(l => l.FechaEntrada)
            .ThenBy(l => l.Id)
            .ToList();

        var asignados = new List<LoteAsignado>();
        var restante = cantidadSolicitada;

        foreach (var lote in vigentes)
        {
            if (restante == 0)
                break;

            var cantidad = Math.Min(restante, lote.Stock);
            asignados.Add(new LoteAsignado(lote, cantidad));
            restante -= cantidad;
        }

        return new PropuestaFefo(cantidadSolicitada, vigentes.Sum(l => l.Stock), asignados);
    }
}
