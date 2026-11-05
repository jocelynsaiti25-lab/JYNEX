using System.Security.Claims;
using System.Text.Encodings.Web;
using EtbotFarma.Application.Features.Auth.ValidarSesion;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EtbotFarma.Api.Seguridad;

/// <summary>
/// Se ejecuta en CADA petición protegida (HU-01 criterio 5):
///   1. Lee la cookie de sesión.
///   2. Envía ValidarSesionCommand (la lógica está en Application, no aquí).
///   3. Si la sesión es válida, identifica al usuario; si no, la API responde 401.
/// </summary>
public sealed class SesionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISender sender) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Esquema = "SesionCookie";
    public const string ClaimNombreCompleto = "nombre_completo";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = CookieSesion.Leer(Request);
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var usuario = await sender.Send(new ValidarSesionCommand(token), Context.RequestAborted);
        if (usuario is null)
        {
            return AuthenticateResult.Fail("Sesión no válida o expirada.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimNombreCompleto, usuario.NombreCompleto),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var identidad = new ClaimsIdentity(claims, Esquema);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema);
        return AuthenticateResult.Success(ticket);
    }
}
