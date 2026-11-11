using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// SQL de la tabla auditoria. Solo guarda datos; no tiene reglas de negocio.
/// Usa la misma ConexionMySql (y su transacción) que los demás repositorios de la petición.
/// </summary>
public sealed class AuditoriaRepository(ConexionMySql conexion) : IAuditoriaRepository
{
    public async Task AgregarAsync(Auditoria auditoria, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auditoria (id_usuario, accion, tabla_afectada, id_registro, valor_anterior, valor_nuevo, fecha)
            VALUES (@idUsuario, @accion, @tablaAfectada, @idRegistro, @valorAnterior, @valorNuevo, @fecha);
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@idUsuario", auditoria.IdUsuario);
        comando.Parameters.AddWithValue("@accion", auditoria.Accion);
        comando.Parameters.AddWithValue("@tablaAfectada", auditoria.TablaAfectada);
        comando.Parameters.AddWithValue("@idRegistro", auditoria.IdRegistro);
        comando.Parameters.AddWithValue("@valorAnterior", (object?)auditoria.ValorAnterior ?? DBNull.Value);
        comando.Parameters.AddWithValue("@valorNuevo", (object?)auditoria.ValorNuevo ?? DBNull.Value);
        comando.Parameters.AddWithValue("@fecha", auditoria.Fecha);

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
