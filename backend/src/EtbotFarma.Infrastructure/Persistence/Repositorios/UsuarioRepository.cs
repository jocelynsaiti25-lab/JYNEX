using System.Data.Common;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla usuario. Solo obtiene y devuelve datos; no tiene reglas de negocio.
/// </summary>
public sealed class UsuarioRepository(ConexionMySql conexion) : IUsuarioRepository
{
    /// <summary>Columnas del usuario y de su rol, para reutilizar en otros SELECT.</summary>
    internal const string ColumnasUsuarioYRol = """
        u.id_usuario, u.nombre, u.apellidos, u.nombre_usuario, u.contrasenia_hash,
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

    /// <summary>Convierte la fila actual (columnas de ColumnasUsuarioYRol) en un Usuario con su Rol.</summary>
    internal static Usuario LeerUsuario(DbDataReader lector)
    {
        var rol = new Rol(
            lector.GetInt32(lector.GetOrdinal("id_rol")),
            lector.GetString(lector.GetOrdinal("nombre_rol")));

        return Usuario.Reconstruir(
            id: lector.GetInt32(lector.GetOrdinal("id_usuario")),
            nombre: lector.GetString(lector.GetOrdinal("nombre")),
            apellidos: lector.GetString(lector.GetOrdinal("apellidos")),
            nombreUsuario: lector.GetString(lector.GetOrdinal("nombre_usuario")),
            contraseniaHash: lector.GetString(lector.GetOrdinal("contrasenia_hash")),
            telefono: lector.GetString(lector.GetOrdinal("telefono")),
            rol: rol,
            activo: lector.GetBoolean(lector.GetOrdinal("activo")),
            fechaRegistro: lector.GetDateTime(lector.GetOrdinal("fecha_registro")));
    }
}
