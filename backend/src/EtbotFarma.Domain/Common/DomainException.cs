namespace EtbotFarma.Domain.Common;

/// <summary>
/// Se lanza cuando se viola una regla de negocio.
/// Ejemplos futuros: "Existencias insuficientes", "El lote está caducado".
/// La API la convierte en una respuesta HTTP 422.
/// </summary>
public class DomainException(string message) : Exception(message);
