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
        // HU-06 escenarios 14 y 15: identidad única por combinación de 4 campos sin importar mayúsculas
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
        // HU-06 escenarios 1, 2 y 16: inserta el medicamento y obtiene el id_medicamento generado
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
}