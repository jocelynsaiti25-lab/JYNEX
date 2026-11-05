using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Infrastructure.Services;

/// <summary>Implementación real: devuelve la hora del servidor.</summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
