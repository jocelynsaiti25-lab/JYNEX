using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla medicamento. Solo lee y guarda datos; sin reglas de negocio.
/// </summary>
public sealed class MedicamentoRepository(ConexionMySql conexion) : IMedicamentoRepository
{
    public async Task<bool> ExisteDuplicadoAsync(
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM medicamento
            WHERE LOWER(TRIM(nombre_comercial)) = LOWER(@nombreComercial)
              AND LOWER(TRIM(nombre_sustancia)) = LOWER(@nombreSustancia)
              AND LOWER(TRIM(presentacion)) = LOWER(@presentacion)
              AND LOWER(TRIM(concentracion)) = LOWER(@concentracion)
            LIMIT 1;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@nombreComercial", nombreComercial.Trim());
        comando.Parameters.AddWithValue("@nombreSustancia", nombreSustancia.Trim());
        comando.Parameters.AddWithValue("@presentacion", presentacion.Trim());
        comando.Parameters.AddWithValue("@concentracion", concentracion.Trim());

        var resultado = await comando.ExecuteScalarAsync(cancellationToken);
        return resultado is not null;
    }

    public async Task<int> AgregarAsync(Medicamento medicamento, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO medicamento (
                nombre_comercial,
                nombre_sustancia,
                presentacion,
                concentracion,
                es_controlado,
                stock_minimo,
                activo,
                fecha_registro
            )
            VALUES (
                @nombreComercial,
                @nombreSustancia,
                @presentacion,
                @concentracion,
                @esControlado,
                @stockMinimo,
                @activo,
                @fechaRegistro
            );
            SELECT LAST_INSERT_ID();
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@nombreComercial", medicamento.NombreComercial);
        comando.Parameters.AddWithValue("@nombreSustancia", medicamento.NombreSustancia);
        comando.Parameters.AddWithValue("@presentacion", medicamento.Presentacion);
        comando.Parameters.AddWithValue("@concentracion", medicamento.Concentracion);
        comando.Parameters.AddWithValue("@esControlado", medicamento.EsControlado);
        comando.Parameters.AddWithValue("@stockMinimo", medicamento.StockMinimo);
        comando.Parameters.AddWithValue("@activo", medicamento.Activo);
        comando.Parameters.AddWithValue("@fechaRegistro", medicamento.FechaRegistro);

        var idGenerado = await comando.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(idGenerado);
    }

    public async Task<Medicamento?> ObtenerPorIdAsync(int idMedicamento, CancellationToken cancellationToken)
    {
        // HU-07 escenarios 1 y 2
        const string sql = """
            SELECT
                id_medicamento,
                nombre_comercial,
                nombre_sustancia,
                presentacion,
                concentracion,
                es_controlado,
                stock_minimo,
                activo,
                fecha_registro
            FROM medicamento
            WHERE id_medicamento = @idMedicamento
            LIMIT 1;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@idMedicamento", idMedicamento);

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken))
            return null;

        return Medicamento.Reconstruir(
            lector.GetInt32("id_medicamento"),
            lector.GetString("nombre_comercial"),
            lector.GetString("nombre_sustancia"),
            lector.GetString("presentacion"),
            lector.GetString("concentracion"),
            lector.GetBoolean("es_controlado"),
            lector.GetInt32("stock_minimo"),
            lector.GetBoolean("activo"),
            lector.GetDateTime("fecha_registro"));
    }

    public async Task<IReadOnlyList<Medicamento>> BuscarActivosAsync(
        string? termino,
        CancellationToken cancellationToken)
    {
        // HU-07 escenarios 1 y 2: busca SOLO medicamentos activos por nombre, sustancia o número de lote
        const string sql = """
            SELECT DISTINCT
                m.id_medicamento,
                m.nombre_comercial,
                m.nombre_sustancia,
                m.presentacion,
                m.concentracion,
                m.es_controlado,
                m.stock_minimo,
                m.activo,
                m.fecha_registro
            FROM medicamento m
            LEFT JOIN lote l ON l.id_medicamento = m.id_medicamento
            WHERE m.activo = TRUE
              AND (
                    @filtro = ''
                    OR LOWER(m.nombre_comercial) LIKE @patron
                    OR LOWER(m.nombre_sustancia) LIKE @patron
                    OR LOWER(l.numero_lote) LIKE @patron
                  )
            ORDER BY m.nombre_comercial ASC, m.concentracion ASC
            LIMIT 50;
            """;

        var filtroNormalizado = termino?.Trim().ToLowerInvariant() ?? string.Empty;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@filtro", filtroNormalizado);
        comando.Parameters.AddWithValue("@patron", $"%{filtroNormalizado}%");

        var resultado = new List<Medicamento>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            resultado.Add(Medicamento.Reconstruir(
                lector.GetInt32("id_medicamento"),
                lector.GetString("nombre_comercial"),
                lector.GetString("nombre_sustancia"),
                lector.GetString("presentacion"),
                lector.GetString("concentracion"),
                lector.GetBoolean("es_controlado"),
                lector.GetInt32("stock_minimo"),
                lector.GetBoolean("activo"),
                lector.GetDateTime("fecha_registro")));
        }

        return resultado;
    }
}