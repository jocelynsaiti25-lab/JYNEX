namespace EtbotFarma.Application.InicioSesion.Dtos;

/// <summary>
/// HU-01: datos del usuario dueño de una sesión.
/// Lo devuelven IniciarSesionHandler, ValidarSesionHandler y ObtenerSesionActualHandler.
/// No lleva el token ni la contraseña: esos datos nunca salen de la capa Application.
/// </summary>
public sealed record UsuarioSesionDto(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
