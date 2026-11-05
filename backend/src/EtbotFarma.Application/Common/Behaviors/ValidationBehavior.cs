using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Validacion;
using MediatR;

namespace EtbotFarma.Application.Common.Behaviors;

/// <summary>
/// Paso del pipeline de MediatR que se ejecuta ANTES de cada handler.
///
///   Controller → Sender.Send(command)
///                    ↓
///              ValidationBehavior  ← aquí
///                    ↓  1. Busca los validadores de ese command (IValidator&lt;TCommand&gt;)
///                    ↓  2. Ejecuta cada uno y junta los errores
///                    ↓  3. ¿Hay errores? Sí → lanza ValidacionException (HTTP 400) y el handler NO se ejecuta
///                    ↓                  No → continúa
///                 Handler
///
/// Gracias a esto, los handlers no tienen ningún "if" de validación.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validadores)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1 y 2. Ejecutar todos los validadores del command y juntar sus errores
        var errores = new List<string>();
        foreach (var validador in validadores)
        {
            errores.AddRange(validador.Validar(request));
        }

        // 3. Si hay errores, se detiene aquí
        if (errores.Count > 0)
        {
            throw new ValidacionException(errores);
        }

        // Sin errores: continúa con el handler
        return await next();
    }
}
