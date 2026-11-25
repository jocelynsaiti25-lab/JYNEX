using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de las tablas movimiento y detalle_movimiento.
/// </summary>
public sealed class MovimientoRepository(ConexionMySql conexion) : IMovimientoRepository
{
    public async Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
    {
        // HU-07 escenario 10: inserta el movimiento de ENTRADA con usuario, fecha y hora
        const string sqlMovimiento = """
            INSERT INTO movimiento (
                tipo,
                id_medicamento,
                id_usuario,
                cantidad_total,
                motivo,
                fecha,
                hora
            )
            VALUES (
                @tipo,
                @idMedicamento,
                @idUsuario,
                @cantidadTotal,
                @motivo,
                @fecha,
                @hora
            );
            SELECT LAST_INSERT_ID();
            """;

        await using var cmdMov = await conexion.CrearComandoAsync(sqlMovimiento, cancellationToken);
        cmdMov.Parameters.AddWithValue("@tipo", movimiento.Tipo);
        cmdMov.Parameters.AddWithValue("@idMedicamento", movimiento.IdMedicamento);
        cmdMov.Parameters.AddWithValue("@idUsuario", movimiento.IdUsuario);
        cmdMov.Parameters.AddWithValue("@cantidadTotal", movimiento.CantidadTotal);
        cmdMov.Parameters.AddWithValue("@motivo", (object?)movimiento.Motivo ?? DBNull.Value);
        cmdMov.Parameters.AddWithValue("@fecha", movimiento.Fecha.Date);
        cmdMov.Parameters.AddWithValue("@hora", movimiento.Fecha.TimeOfDay);

        var idMovimiento = Convert.ToInt32(await cmdMov.ExecuteScalarAsync(cancellationToken));
        movimiento.AsignarId(idMovimiento);

        // HU-07 escenario 10: inserta el detalle asociado al lote
        const string sqlDetalle = """
            INSERT INTO detalle_movimiento (
                id_movimiento,
                id_lote,
                cantidad
            )
            VALUES (
                @idMovimiento,
                @idLote,
                @cantidad
            );
            """;

        foreach (var detalle in movimiento.Detalles)
        {
            await using var cmdDet = await conexion.CrearComandoAsync(sqlDetalle, cancellationToken);
            cmdDet.Parameters.AddWithValue("@idMovimiento", idMovimiento);
            cmdDet.Parameters.AddWithValue("@idLote", detalle.IdLote);
            cmdDet.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
            await cmdDet.ExecuteNonQueryAsync(cancellationToken);
        }

        return idMovimiento;
    }
}