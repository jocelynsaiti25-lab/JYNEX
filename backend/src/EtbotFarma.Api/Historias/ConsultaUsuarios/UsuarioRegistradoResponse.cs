namespace EtbotFarma.Api.Historias.ConsultaUsuarios;

/// <summary>
/// CONTRATO de salida (JSON) de cada cuenta en GET api/usuarios.
/// Nunca incluye la contraseña ni su hash (HU-04 escenario 7).
/// </summary>
public sealed record UsuarioRegistradoResponse(
    int IdUsuario,
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string Telefono,
    string Rol);
