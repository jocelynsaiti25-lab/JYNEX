using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.Features.Ejemplo;

/// <summary>
/// EJEMPLO DE APRENDIZAJE (no es parte de las historias de usuario).
///
/// Producto = los datos que el usuario envía desde el formulario.
/// Es un COMMAND porque representa una acción: "registrar un producto".
///
/// Datos que llegan:
///   Nombre   → texto
///   Precio   → número con decimales
///   Cantidad → número entero
/// </summary>
public sealed record Producto(string Nombre, decimal Precio, int Cantidad) : ICommand<string>;
