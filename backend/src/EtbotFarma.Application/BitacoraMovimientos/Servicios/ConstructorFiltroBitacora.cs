using EtbotFarma.Application.BitacoraMovimientos.Dtos;

namespace EtbotFarma.Application.BitacoraMovimientos.Servicios;

public static class ConstructorFiltroBitacora
{
    public static FiltroBitacoraDto Construir(
        DateOnly? desde,
        DateOnly? hasta,
        int? idUsuario,
        int? idMedicamento,
        string? tipo,
        string zonaHoraria)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria);
        var tipoNormalizado = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim().ToUpperInvariant();

        return new FiltroBitacoraDto(
            desde is { } inicio ? InicioDelDiaEnUtc(inicio, zona) : null,
            hasta is { } fin ? InicioDelDiaEnUtc(fin.AddDays(1), zona) : null,
            idUsuario,
            idMedicamento,
            tipoNormalizado);
    }

    private static DateTime InicioDelDiaEnUtc(DateOnly dia, TimeZoneInfo zona)
    {
        var local = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        if (zona.IsInvalidTime(local))
        {
            local = local.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, zona);
    }
}
