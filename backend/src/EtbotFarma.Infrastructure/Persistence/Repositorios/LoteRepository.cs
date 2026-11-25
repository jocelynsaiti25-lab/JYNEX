using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla lote. Solo devuelve datos; sin reglas de negocio.
/// </summary>
public sealed class LoteRepository(ConexionMySql conexion) : ILoteRepository
{
    public async Task<IReadOnlyList<Lote>> ObtenerTodosOrdenadosPorCaducidadAsync(CancellationToken cancellationToken)
    {
        // HU-08 escenario 4: ordenados por fecha_caducidad ascendente
        const string sql = """
            SELECT
                id_lote,
                id_medicamento,
                numero_lote,
                fecha_caducidad,
                stock,
                fecha_entrada
            FROM lote
            ORDER BY fecha_caducidad ASC, id_lote ASC;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        var lotes = new List<Lote>();

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            lotes.Add(Lote.Reconstruir(
                lector.GetInt32("id_lote"),
                lector.GetInt32("id_medicamento"),
                lector.GetString("numero_lote"),
                DateOnly.FromDateTime(lector.GetDateTime("fecha_caducidad")),
                lector.GetInt32("stock"),
                DateOnly.FromDateTime(lector.GetDateTime("fecha_entrada"))));
        }

        return lotes;
    }
}