using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>
/// Acceso a las sesiones. La implementación está en Infrastructure.
/// </summary>
public interface ISesionRepository
{
    Task AgregarAsync(Sesion sesion, CancellationToken cancellationToken);

    /// <summary>Devuelve la sesión (con su usuario y rol) o null si no existe.</summary>
    Task<Sesion?> ObtenerPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}
