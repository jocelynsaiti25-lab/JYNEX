using EtbotFarma.Application.BajaLotes.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.BajaLotes;

[ApiController]
[Route("api/inventario/lotes")]
public sealed class BajaLotesController(ISender sender) : ControllerBase
{
    [HttpPost("{idLote:int}/baja")]
    public async Task<ActionResult<DarDeBajaLoteResponse>> DarDeBaja(
        int idLote,
        [FromBody] DarDeBajaLoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new DarDeBajaLoteCommand(
            idLote,
            request.CantidadConsultada ?? 0,
            request.Motivo ?? string.Empty);

        var resultado = await sender.Send(command, cancellationToken);

        return Ok(new DarDeBajaLoteResponse(
            resultado.IdLote,
            resultado.IdMedicamento,
            resultado.NombreMedicamento,
            resultado.NumeroLote,
            resultado.FechaCaducidad.ToString("yyyy-MM-dd"),
            resultado.CantidadRetirada,
            resultado.Motivo,
            resultado.Mensaje));
    }
}
