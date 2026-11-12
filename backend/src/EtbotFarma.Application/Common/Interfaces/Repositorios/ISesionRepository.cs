using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>
/// Acceso a las sesiones. La implementación (SQL) está en Infrastructure.
/// Los cambios quedan confirmados hasta que el handler llama a IUnitOfWork.GuardarCambiosAsync.
/// </summary>
public interface ISesionRepository
{
    /// <summary>INSERT de una sesión nueva.</summary>
    Task AgregarAsync(Sesion sesion, CancellationToken cancellationToken);

    /// <summary>Devuelve la sesión (con su usuario y rol) o null si no existe.</summary>
    Task<Sesion?> ObtenerPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>UPDATE de actividad, expiración y cierre de una sesión existente.</summary>
    Task ActualizarAsync(Sesion sesion, CancellationToken cancellationToken);

    /// <summary>
    /// HU-05: cierra TODAS las sesiones abiertas de un usuario (pone la fecha de cierre).
    /// Devuelve cuántas cerró. Queda confirmado con IUnitOfWork.GuardarCambiosAsync.
    /// </summary>
    Task<int> CerrarSesionesDelUsuarioAsync(int idUsuario, DateTime fechaCierre, CancellationToken cancellationToken);
}
