namespace EtbotFarma.Application.ConsultaUsuarios.Dtos;

/// <summary>
/// HU-04 escenario 1: datos de una cuenta en la lista.
/// El correo distingue a dos personas con el mismo nombre (escenario 9).
/// NUNCA lleva la contraseña ni su hash (escenario 7): por eso no se devuelve la entidad Usuario.
/// </summary>
public sealed record UsuarioRegistradoDto(
    int IdUsuario,
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string Telefono,
    string Rol);
