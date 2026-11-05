namespace EtbotFarma.Application.Common.Exceptions;

/// <summary>
/// Credenciales incorrectas o cuenta inactiva.
/// El mensaje es GENÉRICO a propósito: no dice si falló el usuario o la contraseña
/// (HU-01 criterio 4). La API la convierte en HTTP 401.
/// </summary>
public class CredencialesInvalidasException()
    : Exception("Usuario o contraseña incorrectos.");
