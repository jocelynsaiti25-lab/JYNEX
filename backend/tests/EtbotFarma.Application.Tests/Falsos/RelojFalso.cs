using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class RelojFalso(DateTimeOffset ahora) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = ahora;
}
