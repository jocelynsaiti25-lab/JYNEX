using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para el catálogo de medicamentos.</summary>
public interface IMedicamentoRepository
{
    /// <summary>
    /// Verifica si ya existe un medicamento con la misma combinación de nombre comercial,
    /// sustancia activa, presentación y concentración sin importar mayúsculas (HU-06 escenarios 14 y 15).
    /// </summary>
    Task<bool> ExisteDuplicadoAsync(
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        CancellationToken cancellationToken);

    /// <summary>Inserta el medicamento en MySQL y devuelve su id_medicamento generado.</summary>
    Task<int> AgregarAsync(Medicamento medicamento, CancellationToken cancellationToken);

    /// <summary>HU-07 escenarios 1 y 2: obtiene un medicamento por su identificador.</summary>
    Task<Medicamento?> ObtenerPorIdAsync(int idMedicamento, CancellationToken cancellationToken);

    /// <summary>
    /// HU-07 escenarios 1 y 2: busca medicamentos ACTIVOS por nombre comercial, sustancia activa o número de lote.
    /// </summary>
    Task<IReadOnlyList<Medicamento>> BuscarActivosAsync(string? termino, CancellationToken cancellationToken);

    /// <summary>HU-08: obtiene todos los medicamentos activos del catálogo para consulta de inventario.</summary>
    Task<IReadOnlyList<Medicamento>> ObtenerTodosActivosAsync(CancellationToken cancellationToken);

    /// <summary>HU-11 escenario 1: guarda el nuevo umbral de stock mínimo, sin tocar nada más del medicamento.</summary>
    Task ActualizarStockMinimoAsync(int idMedicamento, int stockMinimo, CancellationToken cancellationToken);

    /// <summary>
    /// HU-09 escenarios 8 y 9: igual que ExisteDuplicadoAsync pero EXCLUYE al propio medicamento que se edita,
    /// para permitir guardar cuando solo cambia "Medicamento controlado" o mayúsculas del mismo registro.
    /// </summary>
    Task<bool> ExisteDuplicadoAlEditarAsync(
        int idMedicamento,
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        CancellationToken cancellationToken);

    /// <summary>HU-09 escenarios 1 y 9: guarda los datos descriptivos editados conservando el id_medicamento.</summary>
    Task ActualizarAsync(Medicamento medicamento, CancellationToken cancellationToken);
}