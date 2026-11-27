using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla rol. Solo obtiene y devuelve datos; no tiene reglas de negocio.
/// </summary>
public sealed class RolRepository(ConexionMySql conexion) : IRolRepository
{
    public async Task<Rol?> ObtenerPorIdAsync(int idRol, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id_rol, nombre_rol
            FROM rol
            WHERE id_rol = @idRol
            LIMIT 1;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@idRol", idRol);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken))
        {
            return null;   // no existe
        }

        return Rol.Reconstruir(
            lector.GetInt32(lector.GetOrdinal("id_rol")),
            lector.GetString(lector.GetOrdinal("nombre_rol")));
    }
}
