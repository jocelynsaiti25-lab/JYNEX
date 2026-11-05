namespace EtbotFarma.Application.Dtos.InicioSesion;

/// <summary>
/// Resultado de iniciar sesión.
/// Token es el valor que la API guarda en la cookie; nunca se envía en el JSON.
/// </summary>
public sealed record IniciarSesionDto(
    string Token,
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
