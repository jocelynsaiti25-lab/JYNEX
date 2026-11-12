using EtbotFarma.Application.ReasignacionContrasena.Commands;
using EtbotFarma.Application.ReasignacionContrasena.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.ReasignacionContrasena;

/// <summary>
/// HU-05: reasignación de contraseña. Un controller por historia de usuario.
/// Comparte el prefijo "api/usuarios" con HU-03 y HU-04 porque es el mismo tema.
///
/// El controller SOLO convierte la petición en Command/Query, la envía con MediatR (sender.Send)
/// y convierte el resultado en el contrato de respuesta. No tiene lógica ni try/catch.
///
/// HU-05 escenario 6: el SERVIDOR revisa que sea Administrador ([Authorize(Roles = "Administrador")]):
/// Empleado → 403, sin sesión → 401, y no se cambia ninguna contraseña.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public sealed class ReasignacionContrasenaController(ISender sender) : ControllerBase
{
    /// <summary>
    /// GET api/usuarios/{idUsuario} → datos de la cuenta para el formulario (escenario 2)
    ///   200 → datos · 404 → "La cuenta no existe" · 401/403 → sin permisos
    /// </summary>
    [HttpGet("{idUsuario:int}")]
    public async Task<ActionResult<CuentaResponse>> ObtenerCuenta(int idUsuario, CancellationToken cancellationToken)
    {
        var cuenta = await sender.Send(new ObtenerCuentaQuery(idUsuario), cancellationToken);

        return Ok(new CuentaResponse(cuenta.IdUsuario, cuenta.NombreCompleto, cuenta.NombreUsuario, cuenta.Correo, cuenta.Rol, cuenta.Activo));
    }

    /// <summary>
    /// PUT api/usuarios/{idUsuario}/contrasenia
    ///   200 → contraseña cambiada, sesiones del usuario cerradas y auditoría registrada
    ///   400 → campos vacíos, política de contraseña o "Las contraseñas no coinciden" (validator)
    ///   404 → "La cuenta no existe" · 422 → "La cuenta está desactivada" · 401/403 → sin permisos
    /// </summary>
    [HttpPut("{idUsuario:int}/contrasenia")]
    public async Task<ActionResult<ReasignarContraseniaResponse>> ReasignarContrasenia(
        int idUsuario,
        [FromBody] ReasignarContraseniaRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Request → Command
        var command = new ReasignarContraseniaCommand(
            idUsuario,
            request.NuevaContrasenia ?? string.Empty,
            request.ConfirmarContrasenia ?? string.Empty);

        // 2. MediatR: Validator → Handler
        var resultado = await sender.Send(command, cancellationToken);

        // 3. Dto → Response
        return Ok(new ReasignarContraseniaResponse(
            resultado.IdUsuario, resultado.NombreUsuario, resultado.NombreCompleto, resultado.SesionesCerradas, resultado.EraSuPropiaCuenta));
    }
}
