using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Api.Seguridad;

/// <summary>
/// Implementa ICookieSesionService: escribe la cookie de sesión (HU-01 criterio 6).
///   HttpOnly  → JavaScript no puede leerla (protege contra robo por XSS)
///   Secure    → solo viaja por HTTPS (los navegadores permiten localhost en desarrollo)
///   SameSite  → no se envía desde otros sitios (protege contra CSRF)
/// No tiene fecha de expiración: el servidor decide cuándo expira por inactividad.
/// Vive en la capa Api porque las cookies son algo propio de HTTP.
/// </summary>
public sealed class CookieSesionService(IHttpContextAccessor httpContextAccessor) : ICookieSesionService
{
    public const string NombreCookie = "etbotfarma_sesion";

    public void Guardar(string token)
    {
        var respuesta = httpContextAccessor.HttpContext?.Response
            ?? throw new InvalidOperationException("No hay una petición HTTP activa.");

        respuesta.Cookies.Append(NombreCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        });
    }
}
