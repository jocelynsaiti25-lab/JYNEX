using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>
/// Acceso a los roles (Administrador, Empleado). Application solo conoce esta interfaz;
/// la consulta real a MySQL está en Infrastructure.
/// </summary>
public interface IRolRepository
{
    /// <summary>Devuelve el rol o null si no existe.</summary>
    Task<Rol?> ObtenerPorIdAsync(int idRol, CancellationToken cancellationToken);
}
