using EtbotFarma.Application.BitacoraMovimientos.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

/// <summary>HU-15: consulta de solo lectura, disponible únicamente para Administrador.</summary>
[ApiController]
[Route("api/bitacora/movimientos")]
[Authorize(Roles = "Administrador")]
public sealed class BitacoraMovimientosController(ISender sender) : ControllerBase
{
    /// <summary>GET api/bitacora/movimientos/medicamentos: opciones históricas para el filtro.</summary>
    [HttpGet("medicamentos")]
    public async Task<ActionResult<IReadOnlyList<MedicamentoBitacoraResponse>>> ConsultarMedicamentos(
        CancellationToken cancellationToken)
    {
        var medicamentos = await sender.Send(new ConsultarMedicamentosBitacoraQuery(), cancellationToken);
        return Ok(medicamentos.Select(medicamento => new MedicamentoBitacoraResponse(
            medicamento.IdMedicamento,
            medicamento.NombreComercial,
            medicamento.NombreSustancia)).ToList());
    }

    /// <summary>
    /// GET api/bitacora/movimientos: devuelve movimientos completos con sus lotes asociados.
    /// 400 si los filtros no son válidos; 401 sin sesión; 403 para roles distintos de Administrador.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaBitacoraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        var response = new PaginaBitacoraResponse(
            resultado.Movimientos
                .Select(movimiento => new MovimientoBitacoraResponse(
                    movimiento.IdMovimiento,
                    movimiento.Fecha,
                    movimiento.Usuario,
                    movimiento.Tipo,
                    movimiento.IdMedicamento,
                    movimiento.Medicamento,
                    movimiento.CantidadTotal,
                    movimiento.Motivo,
                    movimiento.Detalles
                        .Select(detalle => new DetalleBitacoraResponse(
                            detalle.IdLote,
                            detalle.NumeroLote,
                            detalle.Cantidad))
                        .ToList()))
                .ToList(),
            resultado.Pagina,
            resultado.TamanoPagina,
            resultado.TotalMovimientos,
            resultado.TotalPaginas);

        return Ok(response);
    }
}
