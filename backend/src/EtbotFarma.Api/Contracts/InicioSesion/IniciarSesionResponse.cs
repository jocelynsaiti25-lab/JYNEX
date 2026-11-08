namespace EtbotFarma.Api.Contracts.InicioSesion;

/// <summary>
/// CONTRATO de salida (JSON) con los datos del usuario que inició sesión.
/// No incluye el token: ese viaja solo en la cookie HttpOnly.
/// </summary>
public sealed record IniciarSesionResponse(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
