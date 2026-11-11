namespace EtbotFarma.Api.Historias.ReasignacionContrasena;

/// <summary>
/// CONTRATO de salida (JSON) de la reasignación. Nunca incluye la contraseña ni su hash.
/// eraSuPropiaCuenta = true → Angular manda al inicio de sesión (HU-05 escenario 17).
/// </summary>
public sealed record ReasignarContraseniaResponse(
    int IdUsuario,
    string NombreUsuario,
    string NombreCompleto,
    int SesionesCerradas,
    bool EraSuPropiaCuenta);
