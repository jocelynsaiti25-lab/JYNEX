namespace EtbotFarma.Api.Middleware;

/// <summary>
/// HU-02 (regla de negocio): las respuestas de la API NO se guardan en la caché del navegador.
/// Así, después de cerrar sesión, el botón Atrás o recargar la página no puede mostrar
/// datos de la sesión anterior (escenarios 3 y 4): el navegador siempre vuelve a preguntar
/// al servidor, y el servidor responde 401 porque la sesión ya está cerrada.
///
/// "no-store" = no guardar la respuesta en ningún lado.
/// </summary>
public sealed class SinCacheMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        // Se pone antes de que responda el controller, así aplica a TODAS las respuestas
        contexto.Response.Headers.CacheControl = "no-store";
        contexto.Response.Headers.Pragma = "no-cache";   // para navegadores antiguos

        await siguiente(contexto);
    }
}
