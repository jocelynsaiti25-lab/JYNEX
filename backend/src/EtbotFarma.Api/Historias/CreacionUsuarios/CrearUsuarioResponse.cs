namespace EtbotFarma.Api.Historias.CreacionUsuarios;

/// <summary>
/// CONTRATO de salida (JSON) con la cuenta creada.
/// Nunca incluye la contraseña (HU-03 criterio 5).
/// </summary>
public sealed record CrearUsuarioResponse(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Correo,
    string Rol);
