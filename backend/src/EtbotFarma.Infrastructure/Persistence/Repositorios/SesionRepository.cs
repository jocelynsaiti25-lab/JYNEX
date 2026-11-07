using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla sesion. Solo obtiene y guarda datos; no tiene reglas de negocio.
/// Los INSERT y UPDATE quedan confirmados cuando el handler llama a GuardarCambiosAsync.
/// </summary>
public sealed class SesionRepository(ConexionMySql conexion) : ISesionRepository
{
    public async Task AgregarAsync(Sesion sesion, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO sesion (id_usuario, token_hash, fecha_inicio, ultima_actividad, fecha_expiracion, fecha_cierre)
            VALUES (@idUsuario, @tokenHash, @fechaInicio, @ultimaActividad, @fechaExpiracion, @fechaCierre);
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@idUsuario", sesion.IdUsuario);
        comando.Parameters.AddWithValue("@tokenHash", sesion.TokenHash);
        comando.Parameters.AddWithValue("@fechaInicio", sesion.FechaInicio);
        comando.Parameters.AddWithValue("@ultimaActividad", sesion.UltimaActividad);
        comando.Parameters.AddWithValue("@fechaExpiracion", sesion.FechaExpiracion);
        comando.Parameters.AddWithValue("@fechaCierre", (object?)sesion.FechaCierre ?? DBNull.Value);

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Sesion?> ObtenerPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT s.id_sesion, s.token_hash, s.fecha_inicio, s.ultima_actividad, s.fecha_expiracion, s.fecha_cierre,
                   {UsuarioRepository.ColumnasUsuarioYRol}
            FROM sesion s
            INNER JOIN usuario u ON u.id_usuario = s.id_usuario
            INNER JOIN rol r ON r.id_rol = u.id_rol
            WHERE s.token_hash = @tokenHash
            LIMIT 1;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@tokenHash", tokenHash);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken))
        {
            return null;   // no existe
        }

        var usuario = UsuarioRepository.LeerUsuario(lector);
        var columnaCierre = lector.GetOrdinal("fecha_cierre");

        return Sesion.Reconstruir(
            id: lector.GetInt32(lector.GetOrdinal("id_sesion")),
            usuario: usuario,
            tokenHash: lector.GetString(lector.GetOrdinal("token_hash")),
            fechaInicio: lector.GetDateTime(lector.GetOrdinal("fecha_inicio")),
            ultimaActividad: lector.GetDateTime(lector.GetOrdinal("ultima_actividad")),
            fechaExpiracion: lector.GetDateTime(lector.GetOrdinal("fecha_expiracion")),
            fechaCierre: lector.IsDBNull(columnaCierre) ? null : lector.GetDateTime(columnaCierre));
    }

    public async Task ActualizarAsync(Sesion sesion, CancellationToken cancellationToken)
    {
        // Se busca por token_hash (es único), así funciona aunque la sesión sea nueva en esta petición
        const string sql = """
            UPDATE sesion
            SET ultima_actividad = @ultimaActividad,
                fecha_expiracion = @fechaExpiracion,
                fecha_cierre = @fechaCierre
            WHERE token_hash = @tokenHash;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@ultimaActividad", sesion.UltimaActividad);
        comando.Parameters.AddWithValue("@fechaExpiracion", sesion.FechaExpiracion);
        comando.Parameters.AddWithValue("@fechaCierre", (object?)sesion.FechaCierre ?? DBNull.Value);
        comando.Parameters.AddWithValue("@tokenHash", sesion.TokenHash);

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
