namespace EtbotFarma.Application.ReasignacionContrasena.Dtos;

/// <summary>
/// HU-05 escenario 2: datos de la cuenta afectada que se muestran antes de cambiar la contraseña.
/// Activo = false → la pantalla avisa "La cuenta está desactivada" (escenario 8).
/// NUNCA lleva la contraseña ni su hash (escenario 4: la anterior no se muestra).
/// </summary>
public sealed record CuentaDto(
    int IdUsuario,
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string Rol,
    bool Activo);
