using System.Security.Claims;
using EtbotFarma.Api.Contracts.InicioSesion;
using EtbotFarma.Api.Seguridad;
using EtbotFarma.Application.Commands.InicioSesion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>
/// HU-01: autenticación e inicio de sesión.
/// Un controller por historia de usuario (así cada integrante trabaja en su propio archivo).
/// El controller solo: recibe el contrato JSON → envía el command → devuelve la respuesta.
/// No tiene lógica de negocio ni acceso a la base de datos.
/// </summary>
[Route("api/sesiones")]
public sealed class InicioSesionController : ApiControllerBase
{
    /// <summary>
    /// POST api/sesiones/iniciar
    ///   200 → sesión creada (cookie + datos del usuario)
    ///   400 → faltan datos
    ///   401 → usuario o contraseña incorrectos (mensaje genérico)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("iniciar")]
    public async Task<ActionResult<IniciarSesionResponse>> IniciarSesion(
        [FromBody] IniciarSesionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IniciarSesionCommand(request.NombreUsuario ?? string.Empty, request.Contrasenia ?? string.Empty);

        var sesion = await Sender.Send(command, cancellationToken);

        CookieSesion.Escribir(Response, sesion.Token);

        return Ok(new IniciarSesionResponse(sesion.IdUsuario, sesion.NombreUsuario, sesion.NombreCompleto, sesion.Rol));
    }

    /// <summary>
    /// GET api/sesiones/actual → datos del usuario con sesión activa.
    /// Angular lo usa para saber si hay sesión (401 si no la hay o expiró).
    /// </summary>
    [HttpGet("actual")]
    public ActionResult<IniciarSesionResponse> ObtenerSesionActual()
    {
        return Ok(new IniciarSesionResponse(
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue(ClaimTypes.Name)!,
            User.FindFirstValue(SesionAuthenticationHandler.ClaimNombreCompleto)!,
            User.FindFirstValue(ClaimTypes.Role)!));
    }
}
