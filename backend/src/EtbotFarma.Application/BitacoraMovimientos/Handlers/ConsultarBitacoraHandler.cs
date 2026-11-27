using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Handlers;

public sealed class ConsultarBitacoraHandler(IMovimientoRepository movimientos, ConfiguracionReloj reloj)
    : IQueryHandler<ConsultarBitacoraQuery, PaginaBitacoraDto>
{
    public async Task<PaginaBitacoraDto> Handle(ConsultarBitacoraQuery query, CancellationToken cancellationToken)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(reloj.ZonaHoraria);
        var tipo = string.IsNullOrWhiteSpace(query.Tipo) ? null : query.Tipo.Trim().ToUpperInvariant();

        var filtro = new FiltroBitacoraDto(
            query.Desde is { } desde ? InicioDelDiaEnUtc(desde, zona) : null,
            query.Hasta is { } hasta ? InicioDelDiaEnUtc(hasta.AddDays(1), zona) : null,
            query.IdUsuario,
            query.IdMedicamento,
            tipo);

        var (lista, total) = await movimientos.ConsultarAsync(filtro, query.Pagina, query.TamanoPagina, cancellationToken);
        var totalPaginas = (int)Math.Ceiling(total / (double)query.TamanoPagina);

        return new PaginaBitacoraDto(lista, query.Pagina, query.TamanoPagina, total, totalPaginas);
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
