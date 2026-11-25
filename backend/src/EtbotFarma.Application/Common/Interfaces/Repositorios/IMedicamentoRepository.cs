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
}