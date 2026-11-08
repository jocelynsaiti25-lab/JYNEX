using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>
/// Acceso a los usuarios. Application solo conoce esta interfaz;
/// la consulta real a MySQL está en Infrastructure.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Devuelve el usuario (con su rol) o null si no existe.</summary>
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken);
}
