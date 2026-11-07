namespace EtbotFarma.Application.Common.Exceptions;

/// <summary>
/// La petición no tiene una sesión válida (no inició sesión o expiró).
/// La API la convierte en HTTP 401.
/// </summary>
public class NoAutenticadoException()
    : Exception("Debes iniciar sesión.");
