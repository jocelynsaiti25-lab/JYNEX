using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Infrastructure.Services;

namespace EtbotFarma.Api.IntegrationTests;

public class DateTimeProviderTests
{
    [Theory]
    [InlineData(2026, 11, 25, 23, 59, 59, 25)]
    [InlineData(2026, 11, 26, 0, 0, 0, 26)]
    public void Con_la_zona_por_defecto_el_dia_cambia_a_medianoche_utc(
        int anio, int mes, int dia, int hora, int minuto, int segundo, int diaEsperado)
    {
        var proveedor = new DateTimeProvider();

        var hoy = proveedor.HoyEn(new DateTimeOffset(anio, mes, dia, hora, minuto, segundo, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 11, diaEsperado), hoy);
    }

    [Theory]
    [InlineData(2026, 11, 26, 5, 59, 59, 25)]
    [InlineData(2026, 11, 26, 6, 0, 0, 26)]
    public void Con_una_zona_configurada_el_dia_cambia_a_su_medianoche_local(
        int anio, int mes, int dia, int hora, int minuto, int segundo, int diaEsperado)
    {
        var proveedor = new DateTimeProvider(new ConfiguracionReloj { ZonaHoraria = "America/Mexico_City" });

        var hoy = proveedor.HoyEn(new DateTimeOffset(anio, mes, dia, hora, minuto, segundo, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 11, diaEsperado), hoy);
    }

    [Fact]
    public void Una_zona_inexistente_falla_al_arrancar_y_no_se_ignora()
    {
        Assert.Throws<TimeZoneNotFoundException>(() =>
            new DateTimeProvider(new ConfiguracionReloj { ZonaHoraria = "Zona/Inexistente" }));
    }
}
