namespace EtbotFarma.Application.Features.Auth.ValidarSesion;

/// <summary>Datos del usuario dueño de una sesión vigente.</summary>
public sealed record UsuarioSesionDto(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
