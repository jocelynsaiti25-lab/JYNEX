using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

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
}