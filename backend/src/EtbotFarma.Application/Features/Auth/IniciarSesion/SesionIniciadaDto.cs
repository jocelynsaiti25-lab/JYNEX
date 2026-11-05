namespace EtbotFarma.Application.Features.Auth.IniciarSesion;

/// <summary>
/// Resultado de iniciar sesión.
/// Token es el valor que la API guarda en la cookie; nunca se envía en el JSON.
/// </summary>
public sealed record SesionIniciadaDto(
    string Token,
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
