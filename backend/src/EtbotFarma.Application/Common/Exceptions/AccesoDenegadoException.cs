namespace EtbotFarma.Application.Common.Exceptions;

/// <summary>
/// El usuario tiene sesión pero su rol no le permite la acción
/// (por ejemplo, un Empleado intentando crear usuarios en HU-03).
/// La API la convierte en HTTP 403.
/// </summary>
public class AccesoDenegadoException(string mensaje = "No tienes permiso para realizar esta acción.")
    : Exception(mensaje);
