using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para los lotes de medicamentos.</summary>
public interface ILoteRepository
{
    /// <summary>Inserta un lote en MySQL y devuelve su id_lote generado.</summary>
    Task<int> AgregarAsync(Lote lote, CancellationToken cancellationToken);
}