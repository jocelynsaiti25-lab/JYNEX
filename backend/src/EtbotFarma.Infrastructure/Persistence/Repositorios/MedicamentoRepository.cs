using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas SQL de la tabla medicamento. Solo lee datos; sin reglas de negocio.
/// </summary>
public sealed class MedicamentoRepository(ConexionMySql conexion) : IMedicamentoRepository
{
    public async Task<IReadOnlyList<Medicamento>> ObtenerTodosActivosAsync(CancellationToken cancellationToken)
    {
        // HU-08 escenarios 1 al 5: obtiene los medicamentos activos ordenados alfabéticamente
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
            WHERE activo = TRUE
            ORDER BY nombre_comercial ASC, concentracion ASC;
            """;

        await using var comando = await conexion.CrearComandoAsync(sql, cancellationToken);
        var lista = new List<Medicamento>();

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            lista.Add(Medicamento.Reconstruir(
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

        return lista;
    }
}