namespace EtbotFarma.Application.CreacionUsuarios.Dtos;

/// <summary>
/// HU-03: datos de la cuenta que se acaba de crear.
/// No lleva la contraseña ni su hash (HU-03 criterio 5).
/// </summary>
public sealed record UsuarioCreadoDto(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    string Correo,
    string Rol);
