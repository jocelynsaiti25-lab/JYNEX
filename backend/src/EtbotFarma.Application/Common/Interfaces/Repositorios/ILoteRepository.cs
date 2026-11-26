using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para los lotes de medicamentos.</summary>
public interface ILoteRepository
{
    /// <summary>Inserta un lote en MySQL y devuelve su id_lote generado.</summary>
    Task<int> AgregarAsync(Lote lote, CancellationToken cancellationToken);

    Task<Lote?> ObtenerPorIdParaActualizarAsync(int idLote, CancellationToken cancellationToken);

    /// <summary>
    /// HU-07 escenarios 6 y 7: verifica si el número de lote ya existe para ese medicamento específico.
    /// </summary>
    Task<bool> ExisteNumeroLoteEnMedicamentoAsync(
        int idMedicamento,
        string numeroLote,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Lote>> ObtenerPorMedicamentosAsync(
        IReadOnlyCollection<int> idsMedicamento,
        CancellationToken cancellationToken);

    /// <summary>
    /// HU-08 escenarios 3 y 4: obtiene todos los lotes ordenados por fecha de caducidad ascendente.
    /// </summary>
    Task<IReadOnlyList<Lote>> ObtenerTodosOrdenadosPorCaducidadAsync(CancellationToken cancellationToken);

    Task<bool> DescontarStockAsync(int idLote, int cantidad, CancellationToken cancellationToken);
}
