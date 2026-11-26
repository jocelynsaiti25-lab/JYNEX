using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Tests.Falsos;

namespace EtbotFarma.Application.Tests.Common;

public class RelojHoyTests
{
    [Theory]
    [InlineData(2026, 11, 25, 0, 0, 0, 2026, 11, 25)]
    [InlineData(2026, 11, 25, 23, 59, 59, 2026, 11, 25)]
    [InlineData(2026, 11, 26, 0, 0, 0, 2026, 11, 26)]
    [InlineData(2026, 12, 31, 23, 59, 59, 2026, 12, 31)]
    [InlineData(2027, 1, 1, 0, 0, 0, 2027, 1, 1)]
    public void Hoy_por_defecto_es_el_dia_utc_y_cambia_a_medianoche(
        int anio, int mes, int dia, int hora, int minuto, int segundo, int anioEsperado, int mesEsperado, int diaEsperado)
    {
        IDateTimeProvider reloj = new RelojFalso(new DateTimeOffset(anio, mes, dia, hora, minuto, segundo, TimeSpan.Zero));

        Assert.Equal(new DateOnly(anioEsperado, mesEsperado, diaEsperado), reloj.Hoy);
    }
}
