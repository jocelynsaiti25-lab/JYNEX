using EtbotFarma.Application.Features.Sistema.ObtenerEstado;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>Endpoints generales del sistema.</summary>
public sealed class SistemaController : ApiControllerBase
{
    /// <summary>GET api/sistema/estado → comprueba que la API responde.</summary>
    [HttpGet("estado")]
    public async Task<ActionResult<EstadoSistemaDto>> ObtenerEstado(CancellationToken cancellationToken)
    {
        var estado = await Sender.Send(new ObtenerEstadoQuery(), cancellationToken);
        return Ok(estado);
    }
}
