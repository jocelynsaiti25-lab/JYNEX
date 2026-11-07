using EtbotFarma.Api.Contracts.InicioSesion;
using EtbotFarma.Application.Commands.InicioSesion;
using EtbotFarma.Application.Queries.InicioSesion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>
/// HU-01: inicio de sesión. Un controller por historia de usuario.
///
/// El controller SOLO hace 3 cosas:
///   1. Recibe el contrato JSON (Request).
///   2. Lo convierte en Command/Query y lo envía con MediatR (sender.Send).
///      MediatR ejecuta primero el Validator y después el Handler.
///   3. Convierte el resultado en el contrato de respuesta (Response) y lo devuelve.
/// No tiene lógica de negocio, ni validaciones, ni cookies, ni base de datos, ni try/catch
/// (los errores los atiende Middleware/GlobalExceptionHandler).
///
/// ControllerBase es la clase de ASP.NET que convierte este archivo en un controller.
/// MediatR (ISender) llega por el constructor: inyección de dependencias.
/// </summary>
[ApiController]
[Route("api/sesiones")]
public sealed class InicioSesionController(ISender sender) : ControllerBase
{
    /// <summary>
    /// POST api/sesiones/iniciar
    ///   200 → sesión creada (cookie segura + datos del usuario)
    ///   400 → faltan datos (IniciarSesionValidator)
    ///   401 → usuario o contraseña incorrectos (mensaje genérico)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("iniciar")]
    public async Task<ActionResult<IniciarSesionResponse>> IniciarSesion(
        [FromBody] IniciarSesionRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Request → Command
        var command = new IniciarSesionCommand(request.NombreUsuario ?? string.Empty, request.Contrasenia ?? string.Empty);

        // 2. MediatR: Validator → Handler
        var usuario = await sender.Send(command, cancellationToken);

        // 3. Dto → Response
        return Ok(new IniciarSesionResponse(usuario.IdUsuario, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Rol));
    }

    /// <summary>
    /// GET api/sesiones/actual → datos del usuario con sesión activa (401 si no la hay o expiró).
    /// Angular lo usa al recargar la página.
    /// </summary>
    [HttpGet("actual")]
    public async Task<ActionResult<IniciarSesionResponse>> ObtenerSesionActual(CancellationToken cancellationToken)
    {
        var usuario = await sender.Send(new ObtenerSesionActualQuery(), cancellationToken);

        return Ok(new IniciarSesionResponse(usuario.IdUsuario, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Rol));
    }
}
