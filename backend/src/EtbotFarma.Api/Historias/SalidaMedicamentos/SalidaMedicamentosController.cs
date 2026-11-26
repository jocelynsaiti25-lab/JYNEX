
using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

[ApiController]
[Route("api/salidas-medicamentos")]
public sealed class SalidaMedicamentosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// GET api/salidas-medicamentos/propuesta
    /// Consulta los lotes disponibles siguiendo FEFO.
    /// </summary>
    [HttpGet("propuesta")]
    public async Task<IActionResult> ProponerSalida(
        [FromQuery] int idMedicamento,
        [FromQuery] int cantidad,
        CancellationToken cancellationToken)
    {
        var resultado = await sender.Send(
            new ProponerSalidaMedicamentoQuery(idMedicamento, cantidad),
            cancellationToken);

        var response = new PropuestaSalidaResponse(
            resultado.IdMedicamento,
            resultado.NombreMedicamento,
            resultado.CantidadSolicitada,
            resultado.CantidadDisponible,
            resultado.Lotes.Select(l => new LoteSalidaResponse(
                l.IdLote,
                l.NumeroLote,
                l.FechaCaducidad.ToString("yyyy-MM-dd"),
                l.StockDisponible,
                l.CantidadAUtilizar)).ToList(),
            resultado.PuedeRealizarse);

        return Ok(response);
    }

    /// <summary>
    /// POST api/salidas-medicamentos
    /// Confirma la salida y registra el movimiento en bitácora.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RegistrarSalidaMedicamentoResponse>> Registrar(
        [FromBody] RegistrarSalidaMedicamentoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegistrarSalidaMedicamentoCommand(
            request.IdMedicamento ?? 0,
            request.Cantidad ?? 0);

        var resultado = await sender.Send(command, cancellationToken);

        var response = new RegistrarSalidaMedicamentoResponse(
            resultado.IdMovimiento,
            resultado.IdMedicamento,
            resultado.NombreMedicamento,
            resultado.CantidadRetirada,
            resultado.LotesUtilizados.Select(l => new LoteSalidaResponse(
                l.IdLote,
                l.NumeroLote,
                l.FechaCaducidad.ToString("yyyy-MM-dd"),
                l.StockDisponible,
                l.CantidadAUtilizar)).ToList(),
            resultado.ExistenciasTotalesActualizadas,
            resultado.SemaforoGeneralActualizado,
            resultado.EstaAgotado,
            resultado.EnStockBajo,
            resultado.Mensaje);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
