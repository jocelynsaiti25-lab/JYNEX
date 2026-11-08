using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class SesionRepositoryFalso : ISesionRepository
{
    public List<Sesion> Sesiones { get; } = [];

    public Task AgregarAsync(Sesion sesion, CancellationToken cancellationToken)
    {
        Sesiones.Add(sesion);
        return Task.CompletedTask;
    }

    public Task<Sesion?> ObtenerPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => Task.FromResult(Sesiones.FirstOrDefault(s => s.TokenHash == tokenHash));

    public int VecesActualizada { get; private set; }

    public Task ActualizarAsync(Sesion sesion, CancellationToken cancellationToken)
    {
        VecesActualizada++;
        return Task.CompletedTask;
    }
}
