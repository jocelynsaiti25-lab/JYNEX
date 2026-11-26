
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla lote. Solo guarda y devuelve datos;
/// sin reglas de negocio.
/// </summary>
public sealed class LoteRepository(ConexionMySql conexion) : ILoteRepository
{
    public async Task<int> AgregarAsync(
        Lote lote,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO lote (
                id_medicamento,
                numero_lote,
                fecha_caducidad,
                stock,
                fecha_entrada
            )
            VALUES (
                @idMedicamento,
                @numeroLote,
                @fechaCaducidad,
                @stock,
                @fechaEntrada
            );
            SELECT LAST_INSERT_ID();
            """;

        await using var comando =
            await conexion.CrearComandoAsync(sql, cancellationToken);

        comando.Parameters.AddWithValue("@idMedicamento", lote.IdMedicamento);
        comando.Parameters.AddWithValue("@numeroLote", lote.NumeroLote);
        comando.Parameters.AddWithValue("@fechaCaducidad",
            lote.FechaCaducidad.ToDateTime(TimeOnly.MinValue));
        comando.Parameters.AddWithValue("@stock", lote.Stock);
        comando.Parameters.AddWithValue("@fechaEntrada",
            lote.FechaEntrada.ToDateTime(TimeOnly.MinValue));

        var idGenerado = await comando.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(idGenerado);
    }

    public async Task<bool> ExisteNumeroLoteEnMedicamentoAsync(
        int idMedicamento,
        string numeroLote,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM lote
            WHERE id_medicamento = @idMedicamento
              AND LOWER(TRIM(numero_lote)) = LOWER(@numeroLote)
            LIMIT 1;
            """;

        await using var comando =
            await conexion.CrearComandoAsync(sql, cancellationToken);

        comando.Parameters.AddWithValue("@idMedicamento", idMedicamento);
        comando.Parameters.AddWithValue("@numeroLote", numeroLote.Trim());

        var resultado = await comando.ExecuteScalarAsync(cancellationToken);
        return resultado is not null;
    }

    public async Task<IReadOnlyList<Lote>> ObtenerPorMedicamentosAsync(
        IReadOnlyCollection<int> idsMedicamento,
        CancellationToken cancellationToken)
    {
        if (idsMedicamento.Count == 0)
            return [];

        var parametros = idsMedicamento
            .Select((_, i) => $"@id{i}")
            .ToList();

        var sql = $"""
            SELECT
                id_lote,
                id_medicamento,
                numero_lote,
                fecha_caducidad,
                stock,
                fecha_entrada
            FROM lote
            WHERE id_medicamento IN ({string.Join(", ", parametros)})
            ORDER BY fecha_caducidad ASC;
            """;

        await using var comando =
            await conexion.CrearComandoAsync(sql, cancellationToken);

        var indice = 0;

        foreach (var id in idsMedicamento)
        {
            comando.Parameters.AddWithValue(parametros[indice++], id);
        }

        var lotes = new List<Lote>();

        await using var lector =
            await comando.ExecuteReaderAsync(cancellationToken);

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

    public async Task<IReadOnlyList<Lote>> ObtenerTodosOrdenadosPorCaducidadAsync(
        CancellationToken cancellationToken)
    {
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

        await using var comando =
            await conexion.CrearComandoAsync(sql, cancellationToken);

        var lotes = new List<Lote>();

        await using var lector =
            await comando.ExecuteReaderAsync(cancellationToken);

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

    /// <summary>
    /// HU-13: obtiene lotes vigentes con existencias,
    /// ordenados por FEFO y bloqueados durante la transacción.
    /// </summary>
    public async Task<IReadOnlyList<Lote>> ObtenerVigentesParaSalidaAsync(
        int idMedicamento,
        DateOnly fechaHoy,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id_lote,
                id_medicamento,
                numero_lote,
                fecha_caducidad,
                stock,
                fecha_entrada
            FROM lote
            WHERE id_medicamento = @idMedicamento
              AND fecha_caducidad > @fechaHoy
              AND stock > 0
            ORDER BY fecha_caducidad ASC, id_lote ASC
            FOR UPDATE;
            """;

        await using var comando =
            await conexion.CrearComandoAsync(sql, cancellationToken);

        comando.Parameters.AddWithValue("@idMedicamento", idMedicamento);
        comando.Parameters.AddWithValue(
            "@fechaHoy", fechaHoy.ToDateTime(TimeOnly.MinValue));

        var lotes = new List<Lote>();

        await using var lector =
            await comando.ExecuteReaderAsync(cancellationToken);

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

    /// <summary>
    /// HU-13: descuenta existencias únicamente si hay stock suficiente.
    /// </summary>
    public async Task<bool> DescontarStockAsync(
        int idLote,
        int cantidad,
        CancellationToken cancellationToken)
    {
        if (idLote <= 0 || cantidad <= 0)
            return false;

        const string sql = """
            UPDATE lote
            SET stock = stock - @cantidad
            WHERE id_lote = @idLote
              AND stock >= @cantidad;
            """;

        await using var comando =
            await conexion.CrearComandoAsync(sql, cancellationToken);

        comando.Parameters.AddWithValue("@idLote", idLote);
        comando.Parameters.AddWithValue("@cantidad", cantidad);

        var filasAfectadas =
            await comando.ExecuteNonQueryAsync(cancellationToken);

        return filasAfectadas == 1;
    }
}
