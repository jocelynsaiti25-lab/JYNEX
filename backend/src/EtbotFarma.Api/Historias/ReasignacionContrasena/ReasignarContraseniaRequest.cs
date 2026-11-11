namespace EtbotFarma.Api.Historias.ReasignacionContrasena;

/// <summary>
/// CONTRATO de entrada (JSON) para PUT api/usuarios/{idUsuario}/contrasenia.
/// Solo la nueva contraseña y su confirmación: nunca se pide la anterior (HU-05 escenario 4).
/// Ejemplo: { "nuevaContrasenia": "Farmacia2027", "confirmarContrasenia": "Farmacia2027" }
/// </summary>
public sealed record ReasignarContraseniaRequest(string? NuevaContrasenia, string? ConfirmarContrasenia);
