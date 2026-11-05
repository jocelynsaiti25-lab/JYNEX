namespace EtbotFarma.Api.Contracts.Auth;

/// <summary>
/// CONTRATO de salida (JSON) con los datos del usuario que inició sesión.
/// No incluye el token: ese viaja solo en la cookie HttpOnly.
/// </summary>
public sealed record SesionUsuarioResponse(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
