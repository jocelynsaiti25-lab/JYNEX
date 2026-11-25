using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para el catálogo de medicamentos.</summary>
public interface IMedicamentoRepository
{
    Task<bool> ExisteDuplicadoAsync(
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        CancellationToken cancellationToken);

    Task<int> AgregarAsync(Medicamento medicamento, CancellationToken cancellationToken);

    /// <summary>HU-07 escenarios 1 y 2: obtiene un medicamento por su identificador.</summary>
    Task<Medicamento?> ObtenerPorIdAsync(int idMedicamento, CancellationToken cancellationToken);

    /// <summary>
    /// HU-07 escenarios 1 y 2: busca medicamentos ACTIVOS por nombre comercial, sustancia activa o número de lote.
    /// </summary>
    Task<IReadOnlyList<Medicamento>> BuscarActivosAsync(string? termino, CancellationToken cancellationToken);
}