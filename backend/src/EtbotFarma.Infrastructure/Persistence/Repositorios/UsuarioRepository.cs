using System.Data.Common;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;
using MySqlConnector;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla usuario. Solo obtiene y devuelve datos; no tiene reglas de negocio.
/// </summary>
public sealed class UsuarioRepository(ConexionMySql conexion) : IUsuarioRepository
{
    /// <summary>Columnas del usuario y de su rol, para reutilizar en otros SELECT.</summary>
    internal const string ColumnasUsuarioYRol = """
        u.id_usuario, u.nombre, u.apellidos, u.nombre_usuario, u.correo, u.contrasenia_hash,
        u.telefono, u.activo, u.fecha_registro, r.id_rol, r.nombre_rol
        """;

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT {ColumnasUsuarioYRol}
            FROM usuario u
            INNER JOIN rol r ON r.id_rol = u.id_rol
            WHERE u.nombre_usuario = @nombreUsuario
            LIMIT 1;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken))
        {
            return null;   // no existe
        }

        return LeerUsuario(lector);
    }

    /// <summary>HU-03: ¿ya hay una cuenta con ese nombre de usuario?</summary>
    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken)
        => ExisteAsync("SELECT COUNT(*) FROM usuario WHERE nombre_usuario = @valor;", nombreUsuario, cancellationToken);

    /// <summary>HU-03: ¿ya hay una cuenta con ese correo? (la persona ya está registrada)</summary>
    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken)
        => ExisteAsync("SELECT COUNT(*) FROM usuario WHERE correo = @valor;", correo, cancellationToken);

    /// <summary>
    /// HU-03: guarda un usuario nuevo y devuelve su id (AUTO_INCREMENT).
    /// Queda confirmado cuando el handler llama a GuardarCambiosAsync.
    /// </summary>
    public async Task<int> AgregarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, activo, fecha_registro)
            VALUES (@nombre, @apellidos, @nombreUsuario, @correo, @contraseniaHash, @telefono, @idRol, @activo, @fechaRegistro);
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
        comando.Parameters.AddWithValue("@apellidos", usuario.Apellidos);
        comando.Parameters.AddWithValue("@nombreUsuario", usuario.NombreUsuario);
        comando.Parameters.AddWithValue("@correo", usuario.Correo);
        comando.Parameters.AddWithValue("@contraseniaHash", usuario.ContraseniaHash);
        comando.Parameters.AddWithValue("@telefono", usuario.Telefono);
        comando.Parameters.AddWithValue("@idRol", usuario.IdRol);
        comando.Parameters.AddWithValue("@activo", usuario.Activo);
        comando.Parameters.AddWithValue("@fechaRegistro", usuario.FechaRegistro);

        try
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
            return (int)comando.LastInsertedId;   // id que MySQL le dio (AUTO_INCREMENT)
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            // Respaldo: si dos peticiones guardan al mismo tiempo, la llave UNIQUE de MySQL lo detiene
            // (HU-03: clics repetidos). Se responde 422 en lugar de un error 500.
            throw new DomainException("El nombre de usuario o el correo ya están registrados.");
        }
    }

    /// <summary>Ejecuta un SELECT COUNT(*) con un solo parámetro (@valor) y dice si encontró filas.</summary>
    private async Task<bool> ExisteAsync(string sql, string valor, CancellationToken cancellationToken)
    {
        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@valor", valor);

        return Convert.ToInt64(await comando.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    /// <summary>Convierte la fila actual (columnas de ColumnasUsuarioYRol) en un Usuario con su Rol.</summary>
    internal static Usuario LeerUsuario(DbDataReader lector)
    {
        var rol = Rol.Reconstruir(
            lector.GetInt32(lector.GetOrdinal("id_rol")),
            lector.GetString(lector.GetOrdinal("nombre_rol")));

        return Usuario.Reconstruir(
            id: lector.GetInt32(lector.GetOrdinal("id_usuario")),
            nombre: lector.GetString(lector.GetOrdinal("nombre")),
            apellidos: lector.GetString(lector.GetOrdinal("apellidos")),
            nombreUsuario: lector.GetString(lector.GetOrdinal("nombre_usuario")),
            correo: lector.GetString(lector.GetOrdinal("correo")),
            contraseniaHash: lector.GetString(lector.GetOrdinal("contrasenia_hash")),
            telefono: lector.GetString(lector.GetOrdinal("telefono")),
            rol: rol,
            activo: lector.GetBoolean(lector.GetOrdinal("activo")),
            fechaRegistro: lector.GetDateTime(lector.GetOrdinal("fecha_registro")));
    }
}
