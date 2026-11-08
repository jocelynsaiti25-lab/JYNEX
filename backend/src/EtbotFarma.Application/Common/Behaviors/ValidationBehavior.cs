using EtbotFarma.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace EtbotFarma.Application.Common.Behaviors;

/// <summary>
/// Paso del pipeline de MediatR que se ejecuta ANTES de cada handler.
///
///   Controller → sender.Send(command)
///                    ↓
///              ValidationBehavior  ← aquí
///                    ↓  1. Recibe todos los validators de FluentValidation registrados para ese command
///                    ↓  2. Los ejecuta con ValidateAsync y junta los errores
///                    ↓  3. ¿Hay errores? Sí → lanza ValidacionException (HTTP 400) y el handler NO se ejecuta
///                    ↓                  No → continúa
///                 Handler
///
/// Gracias a esto: los handlers no validan, y nadie crea validators con "new".
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Si el command no tiene validator, continúa directo al handler
        if (!validators.Any())
        {
            return await next();
        }

        // 1 y 2. Ejecutar todos los validators y juntar sus errores
        var contexto = new ValidationContext<TRequest>(request);
        var resultados = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(contexto, cancellationToken)));

        var errores = resultados
            .SelectMany(resultado => resultado.Errors)
            .Select(error => error.ErrorMessage)
            .Distinct()
            .ToList();

        // 3. Si hay errores, se detiene aquí
        if (errores.Count > 0)
        {
            throw new ValidacionException(errores);
        }

        return await next();
    }
}
