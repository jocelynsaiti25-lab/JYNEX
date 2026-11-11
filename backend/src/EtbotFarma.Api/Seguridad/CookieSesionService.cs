using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Api.Seguridad;

/// <summary>
/// Implementa ICookieSesionService: escribe, lee y borra la cookie de sesión.
///   HttpOnly  → JavaScript no puede leerla (protege contra robo por XSS)
///   Secure    → solo viaja por HTTPS (los navegadores permiten localhost en desarrollo)
///   SameSite  → no se envía desde otros sitios (protege contra CSRF)
/// No tiene fecha de expiración: el servidor decide cuándo expira por inactividad.
/// Vive en la capa Api porque las cookies son algo propio de HTTP.
/// </summary>
public sealed class CookieSesionService(IHttpContextAccessor httpContextAccessor) : ICookieSesionService
{
    public const string NombreCookie = "etbotfarma_sesion";

    /// <summary>Mismas opciones al crear y al borrar: si no coinciden, el navegador no la borra.</summary>
    private static CookieOptions Opciones() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true
    };

    private HttpContext Contexto =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No hay una petición HTTP activa.");

    /// <summary>HU-01 criterio 6: guarda el token al iniciar sesión.</summary>
    public void Guardar(string token)
        => Contexto.Response.Cookies.Append(NombreCookie, token, Opciones());

    /// <summary>HU-02: lee el token de la cookie que mandó el navegador (null si no hay).</summary>
    public string? LeerToken()
        => Contexto.Request.Cookies[NombreCookie];

    /// <summary>HU-02: le pide al navegador que borre la cookie de sesión.</summary>
    public void Eliminar()
        => Contexto.Response.Cookies.Delete(NombreCookie, Opciones());
}
