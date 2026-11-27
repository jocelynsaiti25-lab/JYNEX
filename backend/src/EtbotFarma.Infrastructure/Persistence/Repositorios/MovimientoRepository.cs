using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;
using MySqlConnector;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de las tablas movimiento y detalle_movimiento (bitácora).
/// </summary>
public sealed class MovimientoRepository(ConexionMySql conexion) : IMovimientoRepository
{
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

    private const string CondicionesFiltro = """
        (@desde IS NULL OR m.fecha >= @desde)
        AND (@hasta IS NULL OR m.fecha < @hasta)
        AND (@idUsuario IS NULL OR m.id_usuario = @idUsuario)
        AND (@idMedicamento IS NULL OR m.id_medicamento = @idMedicamento)
        AND (@tipo IS NULL OR m.tipo = @tipo)
        """;

    public async Task<(IReadOnlyList<MovimientoBitacoraDto> Movimientos, int Total)> ConsultarAsync(
        FiltroBitacoraDto filtro,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        var total = await ContarAsync(filtro, cancellationToken);
        if (total == 0)
        {
            return (Array.Empty<MovimientoBitacoraDto>(), 0);
        }

        var cabeceras = await LeerPaginaAsync(filtro, pagina, tamanoPagina, cancellationToken);
        var detalles = await LeerDetallesAsync(cabeceras.Select(c => c.IdMovimiento).ToList(), cancellationToken);

        var movimientos = cabeceras
            .Select(c => c with
            {
                Lotes = detalles.TryGetValue(c.IdMovimiento, out var lotes) ? lotes : Array.Empty<DetalleBitacoraDto>()
            })
            .ToList();

        return (movimientos, total);
    }

    public async Task<IReadOnlyList<MedicamentoBitacoraDto>> ListarMedicamentosAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                med.id_medicamento,
                med.nombre_comercial,
                med.nombre_sustancia,
                med.presentacion,
                med.concentracion
            FROM medicamento med
            ORDER BY med.nombre_comercial, med.presentacion, med.concentracion, med.id_medicamento;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        var medicamentos = new List<MedicamentoBitacoraDto>();

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            medicamentos.Add(new MedicamentoBitacoraDto(
                lector.GetInt32("id_medicamento"),
                lector.GetString("nombre_comercial"),
                lector.GetString("nombre_sustancia"),
                lector.GetString("presentacion"),
                lector.GetString("concentracion")));
        }

        return medicamentos;
    }

    private async Task<int> ContarAsync(FiltroBitacoraDto filtro, CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT COUNT(*)
            FROM movimiento m
            WHERE {CondicionesFiltro};
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        AgregarParametrosFiltro(comando, filtro);

        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<List<MovimientoBitacoraDto>> LeerPaginaAsync(
        FiltroBitacoraDto filtro,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT
                m.id_movimiento,
                m.fecha,
                m.id_usuario,
                CONCAT(u.nombre, ' ', u.apellidos) AS usuario,
                m.tipo,
                m.id_medicamento,
                med.nombre_comercial,
                med.presentacion,
                med.concentracion,
                m.cantidad_total,
                CASE WHEN m.tipo = 'BAJA' THEN m.motivo ELSE NULL END AS motivo
            FROM movimiento m
            INNER JOIN usuario u ON u.id_usuario = m.id_usuario
            INNER JOIN medicamento med ON med.id_medicamento = m.id_medicamento
            WHERE {CondicionesFiltro}
            ORDER BY m.fecha DESC, m.id_movimiento DESC
            LIMIT @limite OFFSET @desplazamiento;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        AgregarParametrosFiltro(comando, filtro);
        comando.Parameters.AddWithValue("@limite", tamanoPagina);
        comando.Parameters.AddWithValue("@desplazamiento", (long)(pagina - 1) * tamanoPagina);

        var cabeceras = new List<MovimientoBitacoraDto>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            var motivoOrdinal = lector.GetOrdinal("motivo");
            cabeceras.Add(new MovimientoBitacoraDto(
                lector.GetInt32("id_movimiento"),
                DateTime.SpecifyKind(lector.GetDateTime("fecha"), DateTimeKind.Utc),
                lector.GetInt32("id_usuario"),
                lector.GetString("usuario"),
                lector.GetString("tipo"),
                lector.GetInt32("id_medicamento"),
                lector.GetString("nombre_comercial"),
                lector.GetString("presentacion"),
                lector.GetString("concentracion"),
                lector.GetInt32("cantidad_total"),
                lector.IsDBNull(motivoOrdinal) ? null : lector.GetString(motivoOrdinal),
                []));
        }

        return cabeceras;
    }

    private async Task<Dictionary<int, IReadOnlyList<DetalleBitacoraDto>>> LeerDetallesAsync(
        IReadOnlyList<int> idsMovimiento,
        CancellationToken cancellationToken)
    {
        var marcadores = idsMovimiento.Select((_, indice) => $"@id{indice}").ToList();
        var sql = $"""
            SELECT
                d.id_movimiento,
                d.id_lote,
                l.numero_lote,
                d.cantidad
            FROM detalle_movimiento d
            INNER JOIN lote l ON l.id_lote = d.id_lote
            WHERE d.id_movimiento IN ({string.Join(", ", marcadores)})
            ORDER BY d.id_movimiento, d.id_detalle;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        for (var indice = 0; indice < idsMovimiento.Count; indice++)
        {
            comando.Parameters.AddWithValue(marcadores[indice], idsMovimiento[indice]);
        }

        var porMovimiento = new Dictionary<int, List<DetalleBitacoraDto>>();
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            var idMovimiento = lector.GetInt32("id_movimiento");
            if (!porMovimiento.TryGetValue(idMovimiento, out var lista))
            {
                lista = [];
                porMovimiento[idMovimiento] = lista;
            }

            lista.Add(new DetalleBitacoraDto(
                lector.GetInt32("id_lote"),
                lector.GetString("numero_lote"),
                lector.GetInt32("cantidad")));
        }

        return porMovimiento.ToDictionary(par => par.Key, par => (IReadOnlyList<DetalleBitacoraDto>)par.Value);
    }

    private static void AgregarParametrosFiltro(MySqlCommand comando, FiltroBitacoraDto filtro)
    {
        comando.Parameters.AddWithValue("@desde", (object?)filtro.DesdeUtc ?? DBNull.Value);
        comando.Parameters.AddWithValue("@hasta", (object?)filtro.HastaUtcExclusivo ?? DBNull.Value);
        comando.Parameters.AddWithValue("@idUsuario", (object?)filtro.IdUsuario ?? DBNull.Value);
        comando.Parameters.AddWithValue("@idMedicamento", (object?)filtro.IdMedicamento ?? DBNull.Value);
        comando.Parameters.AddWithValue("@tipo", (object?)filtro.Tipo ?? DBNull.Value);
    }
}
