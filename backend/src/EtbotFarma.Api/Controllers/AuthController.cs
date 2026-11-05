using System.Security.Claims;
using EtbotFarma.Api.Contracts.Auth;
using EtbotFarma.Api.Seguridad;
using EtbotFarma.Application.Features.Auth.IniciarSesion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>
/// HU-01: autenticación.
/// El controller solo: recibe el contrato JSON → envía el command → devuelve la respuesta.
/// No tiene lógica de negocio ni acceso a la base de datos.
/// </summary>
public sealed class AuthController : ApiControllerBase
{
    /// <summary>
    /// POST api/auth/iniciar-sesion
    ///   200 → sesión creada (cookie + datos del usuario)
    ///   400 → faltan datos
    ///   401 → usuario o contraseña incorrectos (mensaje genérico)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("iniciar-sesion")]
    public async Task<ActionResult<SesionUsuarioResponse>> IniciarSesion(
        [FromBody] IniciarSesionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new IniciarSesionCommand(request.NombreUsuario ?? string.Empty, request.Contrasenia ?? string.Empty);

        var sesion = await Sender.Send(command, cancellationToken);

        CookieSesion.Escribir(Response, sesion.Token);

        return Ok(new SesionUsuarioResponse(sesion.IdUsuario, sesion.NombreUsuario, sesion.NombreCompleto, sesion.Rol));
    }

    /// <summary>
    /// GET api/auth/sesion → datos del usuario con sesión activa.
    /// Angular lo usa para saber si hay sesión (401 si no la hay o expiró).
    /// </summary>
    [HttpGet("sesion")]
    public ActionResult<SesionUsuarioResponse> ObtenerSesion()
    {
        return Ok(new SesionUsuarioResponse(
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue(ClaimTypes.Name)!,
            User.FindFirstValue(SesionAuthenticationHandler.ClaimNombreCompleto)!,
            User.FindFirstValue(ClaimTypes.Role)!));
    }
}
