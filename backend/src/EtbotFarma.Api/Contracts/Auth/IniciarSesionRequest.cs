namespace EtbotFarma.Api.Contracts.Auth;

/// <summary>
/// CONTRATO de entrada (JSON) para POST api/auth/iniciar-sesion.
/// Ejemplo:
/// {
///   "nombreUsuario": "admin",
///   "contrasenia": "MiContraseña123"
/// }
/// </summary>
public sealed record IniciarSesionRequest(string? NombreUsuario, string? Contrasenia);
