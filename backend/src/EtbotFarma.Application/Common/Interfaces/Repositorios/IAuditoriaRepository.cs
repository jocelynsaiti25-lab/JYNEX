using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>
/// Guarda los registros de auditoría (tabla auditoria). Es COMPARTIDO: cualquier historia que haga
/// un cambio administrativo lo usa igual (HU-03 crear usuario, HU-05 reasignar contraseña, etc.).
/// El INSERT queda en la MISMA transacción de la petición: se confirma junto con el cambio
/// cuando el handler llama a IUnitOfWork.GuardarCambiosAsync (o no se guarda nada).
/// </summary>
public interface IAuditoriaRepository
{
    Task AgregarAsync(Auditoria auditoria, CancellationToken cancellationToken);
}
