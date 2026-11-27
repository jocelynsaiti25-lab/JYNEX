using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de las tablas movimiento y detalle_movimiento (bitácora).
/// </summary>
public sealed class MovimientoRepository(ConexionMySql conexion) : IMovimientoRepository
{
    public async Task<PaginaBitacoraDto> ConsultarAsync(
        FiltroBitacoraDto filtro,
        CancellationToken cancellationToken)
    {
        const string sqlTotal = """
            SELECT COUNT(*)
            FROM movimiento m
            WHERE (@desdeUtc IS NULL OR m.fecha >= @desdeUtc)
              AND (@hastaUtcExclusivo IS NULL OR m.fecha < @hastaUtcExclusivo)
              AND (@idUsuario IS NULL OR m.id_usuario = @idUsuario)
              AND (@idMedicamento IS NULL OR m.id_medicamento = @idMedicamento)
              AND (@tipo IS NULL OR m.tipo = @tipo);
            """;

        await using var comandoTotal = await conexion.CrearComandoAsync(sqlTotal, cancellationToken);
        AgregarParametrosFiltro(comandoTotal, filtro);
        var totalMovimientos = Convert.ToInt32(await comandoTotal.ExecuteScalarAsync(cancellationToken));

        const string sqlPagina = """
            WITH movimientos_paginados AS (
                SELECT m.id_movimiento
                FROM movimiento m
                WHERE (@desdeUtc IS NULL OR m.fecha >= @desdeUtc)
                  AND (@hastaUtcExclusivo IS NULL OR m.fecha < @hastaUtcExclusivo)
                  AND (@idUsuario IS NULL OR m.id_usuario = @idUsuario)
                  AND (@idMedicamento IS NULL OR m.id_medicamento = @idMedicamento)
                  AND (@tipo IS NULL OR m.tipo = @tipo)
                ORDER BY m.fecha DESC, m.id_movimiento DESC
                LIMIT @tamanoPagina OFFSET @desplazamiento
            )
            SELECT
                m.id_movimiento,
                m.fecha,
                CONCAT_WS(' ', u.nombre, u.apellidos) AS nombre_usuario,
                m.tipo,
                m.id_medicamento,
                med.nombre_comercial,
                m.cantidad_total,
                m.motivo,
                d.id_lote,
                l.numero_lote,
                d.cantidad AS cantidad_detalle
            FROM movimientos_paginados mp
            INNER JOIN movimiento m ON m.id_movimiento = mp.id_movimiento
            INNER JOIN usuario u ON u.id_usuario = m.id_usuario
            INNER JOIN medicamento med ON med.id_medicamento = m.id_medicamento
            LEFT JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento
                AND d.id_medicamento = m.id_medicamento
            LEFT JOIN lote l ON l.id_lote = d.id_lote
                AND l.id_medicamento = d.id_medicamento
            ORDER BY m.fecha DESC, m.id_movimiento DESC, d.id_lote ASC;
            """;

        await using var comandoPagina = await conexion.CrearComandoAsync(sqlPagina, cancellationToken);
        AgregarParametrosFiltro(comandoPagina, filtro);
        comandoPagina.Parameters.AddWithValue("@tamanoPagina", filtro.TamanoPagina);
        comandoPagina.Parameters.AddWithValue("@desplazamiento", (long)(filtro.Pagina - 1) * filtro.TamanoPagina);

        var constructores = new List<MovimientoBitacoraEnConstruccion>();
        var porId = new Dictionary<int, MovimientoBitacoraEnConstruccion>();
        await using (var lector = await comandoPagina.ExecuteReaderAsync(cancellationToken))
        {
            while (await lector.ReadAsync(cancellationToken))
            {
                var idMovimiento = lector.GetInt32("id_movimiento");
                if (!porId.TryGetValue(idMovimiento, out var movimiento))
                {
                    movimiento = new MovimientoBitacoraEnConstruccion(
                        idMovimiento,
                        DateTime.SpecifyKind(lector.GetDateTime("fecha"), DateTimeKind.Utc),
                        lector.GetString("nombre_usuario"),
                        lector.GetString("tipo"),
                        lector.GetInt32("id_medicamento"),
                        lector.GetString("nombre_comercial"),
                        lector.GetInt32("cantidad_total"),
                        lector.IsDBNull(lector.GetOrdinal("motivo")) ? null : lector.GetString("motivo"));
                    porId.Add(idMovimiento, movimiento);
                    constructores.Add(movimiento);
                }

                if (!lector.IsDBNull(lector.GetOrdinal("id_lote")))
                {
                    movimiento.Detalles.Add(new DetalleBitacoraDto(
                        lector.GetInt32("id_lote"),
                        lector.GetString("numero_lote"),
                        lector.GetInt32("cantidad_detalle")));
                }
            }
        }

        var totalPaginas = (int)Math.Ceiling(totalMovimientos / (double)filtro.TamanoPagina);
        var movimientos = constructores.Select(movimiento => movimiento.CrearDto()).ToList();
        return new PaginaBitacoraDto(movimientos, filtro.Pagina, filtro.TamanoPagina, totalMovimientos, totalPaginas);
    }

    public async Task<IReadOnlyList<MedicamentoBitacoraDto>> ConsultarMedicamentosAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT med.id_medicamento, med.nombre_comercial, med.nombre_sustancia
            FROM movimiento m
            INNER JOIN medicamento med ON med.id_medicamento = m.id_medicamento
            ORDER BY med.nombre_comercial, med.nombre_sustancia, med.id_medicamento;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        var medicamentos = new List<MedicamentoBitacoraDto>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            medicamentos.Add(new MedicamentoBitacoraDto(
                lector.GetInt32("id_medicamento"),
                lector.GetString("nombre_comercial"),
                lector.GetString("nombre_sustancia")));
        }

        return medicamentos;
    }

    public async Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
    {
        // HU-06 escenarios 1 y 3: registra el encabezado de ENTRADA en la bitácora
        const string sqlMovimiento = """
            INSERT INTO movimiento (
                tipo,
                id_medicamento,
                id_usuario,
                cantidad_total,
                motivo,
                fecha
            )
            VALUES (
                @tipo,
                @idMedicamento,
                @idUsuario,
                @cantidadTotal,
                @motivo,
                @fecha
            );
            SELECT LAST_INSERT_ID();
            """;

        await using var comandoMovimiento = await conexion.CrearComandoAsync(sqlMovimiento, cancellationToken);
        comandoMovimiento.Parameters.AddWithValue("@tipo", movimiento.Tipo);
        comandoMovimiento.Parameters.AddWithValue("@idMedicamento", movimiento.IdMedicamento);
        comandoMovimiento.Parameters.AddWithValue("@idUsuario", movimiento.IdUsuario);
        comandoMovimiento.Parameters.AddWithValue("@cantidadTotal", movimiento.CantidadTotal);
        comandoMovimiento.Parameters.AddWithValue("@motivo", (object?)movimiento.Motivo ?? DBNull.Value);
        comandoMovimiento.Parameters.AddWithValue("@fecha", movimiento.Fecha);

        var idMovimiento = Convert.ToInt32(await comandoMovimiento.ExecuteScalarAsync(cancellationToken));
        movimiento.AsignarId(idMovimiento);

        // Inserta cada detalle en detalle_movimiento dentro de la misma transacción
        const string sqlDetalle = """
            INSERT INTO detalle_movimiento (
                id_movimiento,
                id_medicamento,
                id_lote,
                cantidad
            )
            VALUES (
                @idMovimiento,
                @idMedicamento,
                @idLote,
                @cantidad
            );
            """;

        foreach (var detalle in movimiento.Detalles)
        {
            await using var comandoDetalle = await conexion.CrearComandoAsync(sqlDetalle, cancellationToken);
            comandoDetalle.Parameters.AddWithValue("@idMovimiento", detalle.IdMovimiento);
            comandoDetalle.Parameters.AddWithValue("@idMedicamento", detalle.IdMedicamento);
            comandoDetalle.Parameters.AddWithValue("@idLote", detalle.IdLote);
            comandoDetalle.Parameters.AddWithValue("@cantidad", detalle.Cantidad);

            await comandoDetalle.ExecuteNonQueryAsync(cancellationToken);
        }

        return idMovimiento;
    }

    private static void AgregarParametrosFiltro(MySqlConnector.MySqlCommand comando, FiltroBitacoraDto filtro)
    {
        comando.Parameters.AddWithValue("@desdeUtc", (object?)filtro.DesdeUtc ?? DBNull.Value);
        comando.Parameters.AddWithValue("@hastaUtcExclusivo", (object?)filtro.HastaUtcExclusivo ?? DBNull.Value);
        comando.Parameters.AddWithValue("@idUsuario", (object?)filtro.IdUsuario ?? DBNull.Value);
        comando.Parameters.AddWithValue("@idMedicamento", (object?)filtro.IdMedicamento ?? DBNull.Value);
        comando.Parameters.AddWithValue("@tipo", (object?)filtro.Tipo ?? DBNull.Value);
    }

    private sealed class MovimientoBitacoraEnConstruccion(
        int idMovimiento,
        DateTime fecha,
        string usuario,
        string tipo,
        int idMedicamento,
        string medicamento,
        int cantidadTotal,
        string? motivo)
    {
        public List<DetalleBitacoraDto> Detalles { get; } = [];

        public MovimientoBitacoraDto CrearDto() => new(
            idMovimiento,
            fecha,
            usuario,
            tipo,
            idMedicamento,
            medicamento,
            cantidadTotal,
            motivo,
            Detalles);
    }
}
