namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// Guarda en la base de datos, en una sola transacción, todos los cambios pendientes.
/// La implementación está en Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
