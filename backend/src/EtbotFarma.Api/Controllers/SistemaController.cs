using EtbotFarma.Application.Dtos.EstadoSistema;
using EtbotFarma.Application.Queries.EstadoSistema;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>
/// Endpoints generales del sistema (no pertenece a una historia de usuario).
/// Sirve para comprobar que la API está encendida. Es público ([AllowAnonymous]).
/// </summary>
[ApiController]
[Route("api/sistema")]
[AllowAnonymous]
public sealed class SistemaController(ISender sender) : ControllerBase
{
    /// <summary>GET api/sistema/estado → 200 con el nombre, versión y hora del servidor.</summary>
    [HttpGet("estado")]
    public async Task<ActionResult<EstadoSistemaDto>> ObtenerEstado(CancellationToken cancellationToken)
    {
        var estado = await sender.Send(new ObtenerEstadoQuery(), cancellationToken);
        return Ok(estado);
    }
}
