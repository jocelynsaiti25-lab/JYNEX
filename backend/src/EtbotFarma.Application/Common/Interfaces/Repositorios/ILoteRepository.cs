using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para los lotes de medicamentos.</summary>
public interface ILoteRepository
{
    Task<int> AgregarAsync(Lote lote, CancellationToken cancellationToken);

    /// <summary>
    /// HU-07 escenarios 6 y 7: verifica si el número de lote ya existe para ese medicamento específico.
    /// </summary>
    Task<bool> ExisteNumeroLoteEnMedicamentoAsync(
        int idMedicamento,
        string numeroLote,
        CancellationToken cancellationToken);

    /// <summary>
    /// HU-07 escenario 11: obtiene todos los lotes del medicamento para recalcular existencias y semáforo.
    /// </summary>
    Task<IReadOnlyList<Lote>> ObtenerPorMedicamentoAsync(
        int idMedicamento,
        CancellationToken cancellationToken);
}