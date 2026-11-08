using System.Security.Claims;
using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Api.Seguridad;

/// <summary>
/// Implementa IUsuarioActual: lee quién hizo la petición a partir de la identidad
/// que dejó SesionAuthenticationHandler. Así ningún controller lee "claims".
/// </summary>
public sealed class UsuarioActual(IHttpContextAccessor httpContextAccessor) : IUsuarioActual
{
    private ClaimsPrincipal? Usuario => httpContextAccessor.HttpContext?.User;

    public int? IdUsuario =>
        int.TryParse(Usuario?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string NombreUsuario => Usuario?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string NombreCompleto => Usuario?.FindFirstValue(SesionAuthenticationHandler.ClaimNombreCompleto) ?? string.Empty;

    public string Rol => Usuario?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
