namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// Transacción de la petición: todo lo que los repositorios escriben (INSERT, UPDATE)
/// queda pendiente hasta GuardarCambiosAsync, que lo confirma JUNTO (COMMIT).
/// Si ocurre un error antes, no se guarda nada (ROLLBACK).
/// La implementación (MySQL) está en Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
