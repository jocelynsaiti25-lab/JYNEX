using EtbotFarma.Application.Features.Ejemplo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Controllers;

/// <summary>
/// EJEMPLO DE APRENDIZAJE: endpoint para probar ProductoValidator.
/// Pruébalo con el archivo EtbotFarma.Api.http.
/// </summary>
[AllowAnonymous]
public sealed class EjemploController : ApiControllerBase
{
    /// <summary>
    /// POST api/ejemplo/producto
    ///   Datos correctos   → 200 con un mensaje
    ///   Datos incorrectos → 400 con los errores de ProductoValidator
    /// </summary>
    [HttpPost("producto")]
    public async Task<ActionResult<string>> RegistrarProducto(
        [FromBody] Producto producto,
        CancellationToken cancellationToken)
    {
        var mensaje = await Sender.Send(producto, cancellationToken);
        return Ok(mensaje);
    }
}
