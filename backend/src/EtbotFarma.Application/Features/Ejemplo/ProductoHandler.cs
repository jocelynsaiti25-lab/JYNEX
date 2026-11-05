using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.Features.Ejemplo;

/// <summary>
/// EJEMPLO DE APRENDIZAJE: handler del Producto.
/// Primero valida los datos y, solo si son correctos, ejecuta la lógica.
/// (En este ejemplo no se guarda en base de datos, solo se devuelve un mensaje.)
/// </summary>
public sealed class ProductoHandler : ICommandHandler<Producto, string>
{
    private readonly ProductoValidator _validator = new();

    public Task<string> Handle(Producto producto, CancellationToken cancellationToken)
    {
        // 1. Validar los datos
        var errores = _validator.Validar(producto);

        // 2. ¿Hay errores?
        if (errores.Count > 0)
        {
            // 3. Sí → detener y enviar los errores (la API responde 400)
            throw new ValidacionException(errores);
        }

        // 4. No → continuar con la lógica del caso de uso
        var mensaje = $"Producto '{producto.Nombre}' válido: precio {producto.Precio}, cantidad {producto.Cantidad}.";
        return Task.FromResult(mensaje);
    }
}
