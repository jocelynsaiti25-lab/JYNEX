using EtbotFarma.Application.CreacionUsuarios.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.CreacionUsuarios;

/// <summary>
/// HU-03: creación de usuarios. Un controller por historia de usuario.
///
/// El controller SOLO hace 3 cosas:
///   1. Recibe el contrato JSON (Request).
///   2. Lo convierte en Command y lo envía con MediatR (sender.Send).
///      MediatR ejecuta primero CrearUsuarioValidator y después CrearUsuarioHandler.
///   3. Convierte el resultado en el contrato de respuesta (Response) y lo devuelve.
///
/// HU-03 criterio 1: solo el Administrador puede crear cuentas
/// ([Authorize(Roles = "Administrador")] → un Empleado recibe 403, sin sesión 401).
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public sealed class CreacionUsuariosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// POST api/usuarios
    ///   201 → cuenta creada
    ///   400 → datos incorrectos (CrearUsuarioValidator: obligatorios, correo, teléfono, contraseña, rol)
    ///   401 → sin sesión · 403 → no es Administrador
    ///   422 → nombre de usuario en uso o correo ya registrado
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CrearUsuarioResponse>> CrearUsuario(
        [FromBody] CrearUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Request → Command
        var command = new CrearUsuarioCommand(
            request.Nombre ?? string.Empty,
            request.Apellidos ?? string.Empty,
            request.NombreUsuario ?? string.Empty,
            request.Correo ?? string.Empty,
            request.Telefono ?? string.Empty,
            request.Contrasenia ?? string.Empty,
            request.IdRol ?? 0);

        // 2. MediatR: Validator → Handler
        var usuario = await sender.Send(command, cancellationToken);

        // 3. Dto → Response (201 Created)
        return StatusCode(StatusCodes.Status201Created, new CrearUsuarioResponse(
            usuario.IdUsuario, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Correo, usuario.Rol));
    }
}
