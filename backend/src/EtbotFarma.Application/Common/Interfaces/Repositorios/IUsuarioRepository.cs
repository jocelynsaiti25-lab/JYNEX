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

    /// <summary>HU-03: true si ya hay una cuenta con ese nombre de usuario.</summary>
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken);

    /// <summary>HU-03: true si ya hay una cuenta con ese correo (la persona ya está registrada).</summary>
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken);

    /// <summary>HU-03: guarda un usuario nuevo y devuelve el id que le dio la base de datos.</summary>
    Task<int> AgregarAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>
    /// HU-04: lista las cuentas (con su rol) ordenadas por nombre.
    /// Si <paramref name="nombreUsuarioContiene"/> trae texto, solo las que lo contienen en el nombre de usuario
    /// (coincidencia parcial y sin importar mayúsculas). null = todas.
    /// </summary>
    Task<IReadOnlyList<Usuario>> ListarAsync(string? nombreUsuarioContiene, CancellationToken cancellationToken);

    /// <summary>HU-05: devuelve el usuario (con su rol) o null si no existe.</summary>
    Task<Usuario?> ObtenerPorIdAsync(int idUsuario, CancellationToken cancellationToken);

    /// <summary>HU-05: UPDATE del hash de la contraseña. Queda confirmado con IUnitOfWork.GuardarCambiosAsync.</summary>
    Task ActualizarContraseniaAsync(Usuario usuario, CancellationToken cancellationToken);
}
