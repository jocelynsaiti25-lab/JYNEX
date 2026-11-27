using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

/// <summary>Guarda en una lista los registros de auditoría que pidió el handler.</summary>
public sealed class AuditoriaRepositoryFalso : IAuditoriaRepository
{
    public List<Auditoria> Registros { get; } = [];

    public Task AgregarAsync(Auditoria auditoria, CancellationToken cancellationToken)
    {
        Registros.Add(auditoria);
        return Task.CompletedTask;
    }
}
