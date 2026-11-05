using EtbotFarma.Application.Dtos.EstadoSistema;
using EtbotFarma.Application.Queries.EstadoSistema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>Endpoints generales del sistema.</summary>
[AllowAnonymous]
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
