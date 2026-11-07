namespace EtbotFarma.Application.Common.Exceptions;

/// <summary>
/// El registro solicitado no existe (por ejemplo, editar un medicamento que no está, HU-09).
/// La API la convierte en HTTP 404.
/// Uso: throw new NoEncontradoException("El medicamento no existe.");
/// </summary>
public class NoEncontradoException(string mensaje)
    : Exception(mensaje);
