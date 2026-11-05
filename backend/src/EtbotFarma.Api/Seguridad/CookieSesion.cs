namespace EtbotFarma.Api.Seguridad;

/// <summary>
/// Escribe y lee la cookie de sesión (HU-01 criterio 6: cookies seguras).
///   HttpOnly  → JavaScript no puede leerla (protege contra robo por XSS)
///   Secure    → solo viaja por HTTPS (los navegadores permiten localhost en desarrollo)
///   SameSite  → no se envía desde otros sitios (protege contra CSRF)
/// No tiene fecha de expiración: el servidor decide cuándo expira por inactividad.
/// </summary>
public static class CookieSesion
{
    public const string Nombre = "etbotfarma_sesion";

    public static void Escribir(HttpResponse response, string token)
    {
        response.Cookies.Append(Nombre, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        });
    }

    public static string? Leer(HttpRequest request)
        => request.Cookies.TryGetValue(Nombre, out var token) ? token : null;
}
