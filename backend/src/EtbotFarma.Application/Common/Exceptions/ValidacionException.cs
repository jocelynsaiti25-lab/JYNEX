namespace EtbotFarma.Application.Common.Exceptions;

/// <summary>
/// Se lanza cuando un validador encuentra errores en los datos.
/// Lleva la lista de mensajes de error.
/// La API la convierte en una respuesta HTTP 400 con esa lista.
///
/// Uso:
///   var errores = _validator.Validar(datos);
///   if (errores.Count > 0)
///   {
///       throw new ValidacionException(errores);
///   }
/// </summary>
public class ValidacionException(List<string> errores)
    : Exception("Los datos enviados no son válidos.")
{
    public List<string> Errores { get; } = errores;
}
