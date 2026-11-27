using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.BitacoraMovimientos.Servicios;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Handlers;

public sealed class ConsultarBitacoraHandler(IMovimientoRepository movimientos, ConfiguracionReloj reloj)
    : IQueryHandler<ConsultarBitacoraQuery, PaginaBitacoraDto>
{
    public async Task<PaginaBitacoraDto> Handle(ConsultarBitacoraQuery query, CancellationToken cancellationToken)
    {
        var filtro = ConstructorFiltroBitacora.Construir(
            query.Desde, query.Hasta, query.IdUsuario, query.IdMedicamento, query.Tipo, reloj.ZonaHoraria);

        var (lista, total) = await movimientos.ConsultarAsync(filtro, query.Pagina, query.TamanoPagina, cancellationToken);
        var totalPaginas = (int)Math.Ceiling(total / (double)query.TamanoPagina);

        return new PaginaBitacoraDto(lista, query.Pagina, query.TamanoPagina, total, totalPaginas);
    }
}
