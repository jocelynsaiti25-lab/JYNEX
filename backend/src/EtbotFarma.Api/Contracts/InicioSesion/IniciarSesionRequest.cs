namespace EtbotFarma.Api.Contracts.InicioSesion;

/// <summary>
/// CONTRATO de entrada (JSON) para POST api/sesiones/iniciar.
/// Ejemplo:
/// {
///   "nombreUsuario": "admin",
///   "contrasenia": "MiContraseña123"
/// }
/// </summary>
public sealed record IniciarSesionRequest(string? NombreUsuario, string? Contrasenia);
