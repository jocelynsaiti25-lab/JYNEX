using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para los lotes de medicamentos.</summary>
public interface ILoteRepository
{
    /// <summary>
    /// HU-08 escenarios 3 y 4: obtiene todos los lotes ordenados por fecha de caducidad ascendente.
    /// </summary>
    Task<IReadOnlyList<Lote>> ObtenerTodosOrdenadosPorCaducidadAsync(CancellationToken cancellationToken);
}