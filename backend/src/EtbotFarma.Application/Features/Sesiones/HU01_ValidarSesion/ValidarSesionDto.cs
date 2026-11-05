namespace EtbotFarma.Application.Features.Sesiones.HU01_ValidarSesion;

/// <summary>Datos del usuario dueño de una sesión vigente.</summary>
public sealed record ValidarSesionDto(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
