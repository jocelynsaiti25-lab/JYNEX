using EtbotFarma.Application.CierreSesion.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.CierreSesion;

/// <summary>
/// HU-02: cierre seguro de sesión. Un controller por historia de usuario.
/// Comparte el prefijo "api/sesiones" con HU-01 porque es el mismo tema.
///
/// El controller SOLO envía el command con MediatR: no lee ni borra cookies
/// (eso lo hace CerrarSesionHandler a través de ICookieSesionService).
///
/// [AllowAnonymous]: si la sesión ya expiró (escenario 10), cerrar sesión NO debe dar error 401;
/// el handler simplemente borra la cookie y responde 204.
/// </summary>
[ApiController]
[Route("api/sesiones")]
public sealed class CierreSesionController(ISender sender) : ControllerBase
{
    /// <summary>
    /// POST api/sesiones/cerrar
    ///   204 → sesión cerrada en el servidor y cookie borrada (también si ya no había sesión)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("cerrar")]
    public async Task<IActionResult> CerrarSesion(CancellationToken cancellationToken)
    {
        // 1. MediatR: Handler (no hay validator porque no se reciben datos)
        await sender.Send(new CerrarSesionCommand(), cancellationToken);

        // 2. 204 No Content: todo bien y no hay datos que devolver
        return NoContent();
    }
}
