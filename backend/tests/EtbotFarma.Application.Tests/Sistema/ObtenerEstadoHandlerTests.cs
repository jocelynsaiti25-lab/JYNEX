using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Features.Sistema.ObtenerEstado;

namespace EtbotFarma.Application.Tests.Sistema;

public class ObtenerEstadoHandlerTests
{
    private static readonly DateTimeOffset FechaFija = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Reloj falso: siempre devuelve la misma fecha (gracias a IDateTimeProvider).</summary>
    private sealed class RelojFijo : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => FechaFija;
    }

    [Fact]
    public async Task Devuelve_nombre_version_y_fecha_del_servidor()
    {
        var handler = new ObtenerEstadoHandler(new RelojFijo());

        var resultado = await handler.Handle(new ObtenerEstadoQuery(), CancellationToken.None);

        Assert.Equal("ETBOTFARMA WEB", resultado.Aplicacion);
        Assert.Equal("1.0.0", resultado.Version);
        Assert.Equal(FechaFija, resultado.FechaServidorUtc);
    }
}
