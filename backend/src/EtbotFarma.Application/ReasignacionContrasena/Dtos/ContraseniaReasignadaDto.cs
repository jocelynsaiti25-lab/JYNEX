namespace EtbotFarma.Application.ReasignacionContrasena.Dtos;

/// <summary>
/// HU-05: resultado de la reasignación. NUNCA lleva la contraseña ni su hash.
/// EraSuPropiaCuenta = true cuando el Administrador cambió SU contraseña:
/// su sesión actual también se cerró y debe volver a iniciar sesión (escenario 17).
/// </summary>
public sealed record ContraseniaReasignadaDto(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    int SesionesCerradas,
    bool EraSuPropiaCuenta);
