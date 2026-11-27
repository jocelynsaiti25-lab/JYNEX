using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.BitacoraMovimientos.Servicios;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Domain.Common;

namespace EtbotFarma.Application.BitacoraMovimientos.Handlers;

public sealed class ExportarBitacoraHandler(
    IMovimientoRepository movimientos,
    IGeneradorExcelBitacora generador,
    ConfiguracionReloj configuracionReloj,
    IDateTimeProvider reloj)
    : IQueryHandler<ExportarBitacoraQuery, ArchivoExportadoDto>
{
    public const int TamanoLote = 500;
    public const string MensajeSinMovimientos = "No hay movimientos para exportar.";

    public async Task<ArchivoExportadoDto> Handle(ExportarBitacoraQuery query, CancellationToken cancellationToken)
    {
        var filtro = ConstructorFiltroBitacora.Construir(
            query.Desde, query.Hasta, query.IdUsuario, query.IdMedicamento, query.Tipo, configuracionReloj.ZonaHoraria);

        var todos = new List<MovimientoBitacoraDto>();
        var pagina = 1;
        int total;

        do
        {
            var (lote, totalFiltrado) = await movimientos.ConsultarAsync(filtro, pagina, TamanoLote, cancellationToken);
            total = totalFiltrado;
            todos.AddRange(lote);
            pagina++;

            if (lote.Count == 0)
            {
                break;
            }
        }
        while (todos.Count < total);

        if (todos.Count == 0)
        {
            throw new DomainException(MensajeSinMovimientos);
        }

        var contenido = generador.Generar(todos);
        var nombre = $"bitacora-movimientos-{reloj.Hoy:yyyy-MM-dd}.xlsx";

        return new ArchivoExportadoDto(contenido, nombre, todos.Count);
    }
}
