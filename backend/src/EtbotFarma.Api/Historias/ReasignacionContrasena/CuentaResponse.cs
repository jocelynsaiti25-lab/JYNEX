namespace EtbotFarma.Api.Historias.ReasignacionContrasena;

/// <summary>
/// CONTRATO de salida (JSON) de GET api/usuarios/{idUsuario}: datos de la cuenta para el formulario
/// de reasignación. Nunca incluye la contraseña ni su hash.
/// </summary>
public sealed record CuentaResponse(
    int IdUsuario,
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string Rol,
    bool Activo);
