using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo zonaOperativa;

    public DateTimeProvider() : this(new ConfiguracionReloj())
    {
    }

    public DateTimeProvider(ConfiguracionReloj configuracion)
    {
        zonaOperativa = TimeZoneInfo.FindSystemTimeZoneById(configuracion.ZonaHoraria);
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Hoy => HoyEn(UtcNow);

    public DateOnly HoyEn(DateTimeOffset instante) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, zonaOperativa).DateTime);
}
