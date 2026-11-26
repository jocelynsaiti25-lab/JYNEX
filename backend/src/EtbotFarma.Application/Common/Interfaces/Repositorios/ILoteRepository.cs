
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>
/// Contrato de persistencia para los lotes de medicamentos.
/// </summary>
public interface ILoteRepository
{
    Task<int> AgregarAsync(
        Lote lote,
        CancellationToken cancellationToken);

    Task<bool> ExisteNumeroLoteEnMedicamentoAsync(
        int idMedicamento,
        string numeroLote,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Lote>> ObtenerPorMedicamentosAsync(
        IReadOnlyCollection<int> idsMedicamento,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Lote>> ObtenerTodosOrdenadosPorCaducidadAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// HU-13: obtiene los lotes vigentes con existencias,
    /// bloqueándolos dentro de la transacción.
    /// </summary>
    Task<IReadOnlyList<Lote>> ObtenerVigentesParaSalidaAsync(
        int idMedicamento,
        DateOnly fechaHoy,
        CancellationToken cancellationToken);

    /// <summary>
    /// HU-13: descuenta existencias sin permitir stock negativo.
    /// </summary>
    Task<bool> DescontarStockAsync(
        int idLote,
        int cantidad,
        CancellationToken cancellationToken);
}
