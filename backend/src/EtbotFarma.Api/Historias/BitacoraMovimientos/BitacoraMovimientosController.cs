using EtbotFarma.Application.BitacoraMovimientos.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

[ApiController]
[Route("api/bitacora")]
[Authorize(Roles = "Administrador")]
public sealed class BitacoraMovimientosController(ISender sender) : ControllerBase
{
    [HttpGet("movimientos")]
    public async Task<ActionResult<PaginaBitacoraResponse>> Consultar(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? idUsuario,
        [FromQuery] int? idMedicamento,
        [FromQuery] string? tipo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await sender.Send(
            new ConsultarBitacoraQuery(desde, hasta, idUsuario, idMedicamento, tipo, pagina, tamanoPagina),
            cancellationToken);

        return Ok(new PaginaBitacoraResponse(
            resultado.Movimientos
                .Select(m => new MovimientoBitacoraResponse(
                    m.IdMovimiento,
                    m.FechaUtc,
                    m.IdUsuario,
                    m.Usuario,
                    m.Tipo,
                    m.IdMedicamento,
                    m.Medicamento,
                    m.Presentacion,
                    m.Concentracion,
                    m.CantidadTotal,
                    m.Motivo,
                    m.Lotes
                        .Select(l => new DetalleBitacoraResponse(l.IdLote, l.NumeroLote, l.Cantidad))
                        .ToList()))
                .ToList(),
            resultado.Pagina,
            resultado.TamanoPagina,
            resultado.Total,
            resultado.TotalPaginas));
    }

    [HttpGet("medicamentos")]
    public async Task<ActionResult<IReadOnlyList<MedicamentoBitacoraResponse>>> ListarMedicamentos(
        CancellationToken cancellationToken)
    {
        var medicamentos = await sender.Send(new ConsultarMedicamentosBitacoraQuery(), cancellationToken);

        return Ok(medicamentos
            .Select(m => new MedicamentoBitacoraResponse(
                m.IdMedicamento, m.NombreComercial, m.NombreSustancia, m.Presentacion, m.Concentracion))
            .ToList());
    }
}
