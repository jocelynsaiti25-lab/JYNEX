using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class UnitOfWorkFalso : IUnitOfWork
{
    public int VecesGuardado { get; private set; }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        VecesGuardado++;
        return Task.CompletedTask;
    }
}
