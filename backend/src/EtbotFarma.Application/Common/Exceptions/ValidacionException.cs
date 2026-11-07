namespace EtbotFarma.Application.Common.Exceptions;

/// <summary>
/// Datos de entrada incorrectos. La lanza SOLO ValidationBehavior con los
/// mensajes de FluentValidation. La API la convierte en HTTP 400 con la lista "errores".
/// </summary>
public class ValidacionException(List<string> errores)
    : Exception("Los datos enviados no son válidos.")
{
    public List<string> Errores { get; } = errores;
}
