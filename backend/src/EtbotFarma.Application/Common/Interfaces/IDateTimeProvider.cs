namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// Abstracción de la fecha y hora actual (principio D de SOLID).
/// En lugar de usar DateTime.Now directamente, los handlers piden esta interfaz.
/// Ventaja: en las pruebas se puede usar una fecha fija (útil para el semáforo de caducidad).
/// La implementación real está en Infrastructure.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
