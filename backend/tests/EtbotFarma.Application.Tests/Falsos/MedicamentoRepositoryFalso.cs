using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class MedicamentoRepositoryFalso : IMedicamentoRepository
{
    public bool SimularDuplicado { get; init; }
    public bool SimularDuplicadoAlEditar { get; init; }
    public List<Medicamento> Medicamentos { get; } = [];
    public List<Medicamento> Actualizados { get; } = [];

    public Task<bool> ExisteDuplicadoAsync(
        string nombreComercial, string nombreSustancia, string presentacion, string concentracion, CancellationToken cancellationToken)
        => Task.FromResult(SimularDuplicado);

    public Task<int> AgregarAsync(Medicamento medicamento, CancellationToken cancellationToken)
    {
        Medicamentos.Add(medicamento);
        return Task.FromResult(Medicamentos.Count);
    }

    public Task<Medicamento?> ObtenerPorIdAsync(int idMedicamento, CancellationToken cancellationToken)
        => Task.FromResult(Medicamentos.FirstOrDefault(m => m.Id == idMedicamento));

    public Task<IReadOnlyList<Medicamento>> BuscarActivosAsync(string? termino, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Medicamento>>(Medicamentos.Where(m => m.Activo).ToList());

    public Task<IReadOnlyList<Medicamento>> ObtenerTodosActivosAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Medicamento>>(Medicamentos.Where(m => m.Activo).ToList());

    public Task<bool> ExisteDuplicadoAlEditarAsync(
        int idMedicamento, string nombreComercial, string nombreSustancia, string presentacion, string concentracion, CancellationToken cancellationToken)
        => Task.FromResult(SimularDuplicadoAlEditar);

    public Task ActualizarAsync(Medicamento medicamento, CancellationToken cancellationToken)
    {
        Actualizados.Add(medicamento);
        return Task.CompletedTask;
    }
}
