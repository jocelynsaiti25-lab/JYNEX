using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Handlers;

/// <summary>Convierte las fechas de calendario operativas a límites UTC y consulta la bitácora.</summary>
public sealed class ConsultarBitacoraHandler(
    IMovimientoRepository movimientos,
    ConfiguracionReloj configuracionReloj)
    : IQueryHandler<ConsultarBitacoraQuery, PaginaBitacoraDto>
{
    public Task<PaginaBitacoraDto> Handle(ConsultarBitacoraQuery query, CancellationToken cancellationToken)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(configuracionReloj.ZonaHoraria);
        DateTime? desdeUtc = query.Desde is DateOnly desde ? AUtc(desde, zona) : null;
        DateTime? hastaUtcExclusivo = query.Hasta is DateOnly hasta ? AUtc(hasta.AddDays(1), zona) : null;
        var tipo = string.IsNullOrWhiteSpace(query.Tipo) ? null : query.Tipo.Trim().ToUpperInvariant();

        return movimientos.ConsultarAsync(
            new FiltroBitacoraDto(
                desdeUtc,
                hastaUtcExclusivo,
                query.IdUsuario,
                query.IdMedicamento,
                tipo,
                query.Pagina,
                query.TamanoPagina),
            cancellationToken);
    }

    private static DateTime AUtc(DateOnly fecha, TimeZoneInfo zona)
    {
        var inicioLocal = DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(inicioLocal, zona);
    }
}
