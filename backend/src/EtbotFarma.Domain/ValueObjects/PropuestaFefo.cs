using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.ValueObjects;

public sealed record PropuestaFefo(
    IReadOnlyList<AsignacionLote> Asignaciones,
    int CantidadTotal,
    int ExistenciasVigentes)
{
    public const string MensajeCantidadInvalida = "La cantidad debe ser un número entero mayor que 0";

    public static IReadOnlyList<Lote> LotesVigentesEnOrden(IEnumerable<Lote> lotes, DateOnly hoy) =>
        lotes
            .Where(lote => lote.Stock > 0 && !lote.EstaCaducado(hoy))
            .OrderBy(lote => lote.FechaCaducidad)
            .ThenBy(lote => lote.Id)
            .ToList();

    public static PropuestaFefo Calcular(IEnumerable<Lote> lotes, int cantidad, DateOnly hoy)
    {
        if (cantidad <= 0)
            throw new DomainException(MensajeCantidadInvalida);

        var todos = lotes.ToList();
        var vigentes = LotesVigentesEnOrden(todos, hoy);
        var disponibles = vigentes.Sum(lote => lote.Stock);

        if (disponibles == 0)
        {
            var soloVencidos = todos.Any(lote => lote.Stock > 0 && lote.EstaCaducado(hoy));
            throw new DomainException(soloVencidos
                ? "Sin existencias: el medicamento solo tiene lotes vencidos con unidades y los lotes vencidos no se pueden usar."
                : "Sin existencias: el medicamento no tiene unidades en lotes vigentes.");
        }

        if (cantidad > disponibles)
            throw new DomainException(
                $"Stock insuficiente: solo hay {disponibles} unidades disponibles en lotes vigentes.");

        var asignaciones = new List<AsignacionLote>();
        var pendiente = cantidad;

        foreach (var lote in vigentes)
        {
            if (pendiente == 0)
                break;

            var tomadas = Math.Min(lote.Stock, pendiente);
            asignaciones.Add(new AsignacionLote(lote.Id, lote.NumeroLote, lote.FechaCaducidad, tomadas));
            pendiente -= tomadas;
        }

        return new PropuestaFefo(asignaciones, cantidad, disponibles);
    }
}
