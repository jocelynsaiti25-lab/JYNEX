using EtbotFarma.Application.BitacoraMovimientos.Handlers;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Tests.Falsos;

namespace EtbotFarma.Application.Tests.BitacoraMovimientos.Handlers;

public class ConsultarBitacoraHandlerTests
{
    [Fact]
    public async Task Convierte_los_dias_locales_a_limites_UTC_y_normaliza_tipo()
    {
        var repositorio = new MovimientoRepositoryFalso();
        var handler = new ConsultarBitacoraHandler(repositorio, new ConfiguracionReloj { ZonaHoraria = "UTC" });

        await handler.Handle(
            new ConsultarBitacoraQuery(
                new DateOnly(2026, 11, 25), new DateOnly(2026, 11, 26), 4, 9, " salida ", 2, 15),
            CancellationToken.None);

        Assert.NotNull(repositorio.FiltroConsultado);
        Assert.Equal(new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc), repositorio.FiltroConsultado.DesdeUtc);
        Assert.Equal(new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc), repositorio.FiltroConsultado.HastaUtcExclusivo);
        Assert.Equal(4, repositorio.FiltroConsultado.IdUsuario);
        Assert.Equal(9, repositorio.FiltroConsultado.IdMedicamento);
        Assert.Equal("SALIDA", repositorio.FiltroConsultado.Tipo);
        Assert.Equal(2, repositorio.FiltroConsultado.Pagina);
        Assert.Equal(15, repositorio.FiltroConsultado.TamanoPagina);
    }

    [Fact]
    public async Task Sin_filtros_envia_limites_y_tipo_nulos()
    {
        var repositorio = new MovimientoRepositoryFalso();
        var handler = new ConsultarBitacoraHandler(repositorio, new ConfiguracionReloj { ZonaHoraria = "UTC" });

        await handler.Handle(new ConsultarBitacoraQuery(null, null, null, null, null), CancellationToken.None);

        Assert.NotNull(repositorio.FiltroConsultado);
        Assert.Null(repositorio.FiltroConsultado.DesdeUtc);
        Assert.Null(repositorio.FiltroConsultado.HastaUtcExclusivo);
        Assert.Null(repositorio.FiltroConsultado.Tipo);
    }
}
