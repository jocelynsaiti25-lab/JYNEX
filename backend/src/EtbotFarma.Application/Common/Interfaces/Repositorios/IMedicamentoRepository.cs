using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para el catálogo de medicamentos.</summary>
public interface IMedicamentoRepository
{
    /// <summary>HU-08: obtiene todos los medicamentos activos del catálogo para consulta de inventario.</summary>
    Task<IReadOnlyList<Medicamento>> ObtenerTodosActivosAsync(CancellationToken cancellationToken);
}