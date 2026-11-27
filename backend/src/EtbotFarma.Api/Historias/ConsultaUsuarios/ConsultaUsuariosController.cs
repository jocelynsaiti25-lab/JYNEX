using EtbotFarma.Application.ConsultaUsuarios.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.ConsultaUsuarios;

/// <summary>
/// HU-04: consulta de usuarios registrados. Un controller por historia de usuario.
/// Comparte el prefijo "api/usuarios" con HU-03 (crear: POST; consultar: GET).
///
/// El controller SOLO hace 3 cosas:
///   1. Recibe la búsqueda de la URL (?busqueda=lopez).
///   2. La convierte en Query y la envía con MediatR (sender.Send).
///   3. Convierte los Dto en el contrato de respuesta y los devuelve.
///
/// HU-04 escenario 3: el SERVIDOR revisa que sea Administrador antes de devolver cualquier dato
/// ([Authorize(Roles = "Administrador")] → Empleado 403, sin sesión 401). Ocultar el menú no basta.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public sealed class ConsultaUsuariosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// GET api/usuarios?busqueda=lopez
    ///   200 → lista de cuentas (vacía si no hay coincidencias)
    ///   400 → búsqueda de más de 50 caracteres (ConsultarUsuariosValidator)
    ///   401 → sin sesión · 403 → no es Administrador
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioRegistradoResponse>>> ConsultarUsuarios(
        [FromQuery] string? busqueda,
        CancellationToken cancellationToken)
    {
        // 1. Petición → Query
        var query = new ConsultarUsuariosQuery(busqueda);

        // 2. MediatR: Validator → Handler
        var usuarios = await sender.Send(query, cancellationToken);

        // 3. Dto → Response
        return Ok(usuarios
            .Select(u => new UsuarioRegistradoResponse(u.IdUsuario, u.NombreCompleto, u.NombreUsuario, u.Correo, u.Telefono, u.Rol))
            .ToList());
    }
}
