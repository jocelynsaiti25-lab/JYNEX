using FluentValidation;
using MediatR;

namespace EtbotFarma.Application.Common.Behaviors;

/// <summary>
/// Paso del pipeline de MediatR que se ejecuta ANTES de cada handler.
/// 1. Busca los validadores (FluentValidation) del comando o consulta.
/// 2. Si alguna regla falla, lanza ValidationException y el handler NO se ejecuta.
/// 3. Si todo es válido, continúa con el handler.
/// Así ningún handler tiene que validar manualmente el formato de los datos.
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
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var resultados = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errores = resultados
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (errores.Count != 0)
        {
            throw new ValidationException(errores);
        }

        return await next();
    }
}
