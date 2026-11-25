using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla lote. Solo guarda y devuelve datos; sin reglas de negocio.
/// </summary>
public sealed class LoteRepository(ConexionMySql conexion) : ILoteRepository
{
    public async Task<int> AgregarAsync(Lote lote, CancellationToken cancellationToken)
    {
        // HU-06 escenarios 1, 2 y 3: guarda el lote inicial del medicamento
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

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        comando.Parameters.AddWithValue("@idMedicamento", lote.IdMedicamento);
        comando.Parameters.AddWithValue("@numeroLote", lote.NumeroLote);
        comando.Parameters.AddWithValue("@fechaCaducidad", lote.FechaCaducidad.ToDateTime(TimeOnly.MinValue));
        comando.Parameters.AddWithValue("@stock", lote.Stock);
        comando.Parameters.AddWithValue("@fechaEntrada", lote.FechaEntrada.ToDateTime(TimeOnly.MinValue));

        var idGenerado = await comando.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(idGenerado);
    }
}