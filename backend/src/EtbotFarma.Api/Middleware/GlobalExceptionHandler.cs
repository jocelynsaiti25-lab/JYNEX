using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Middleware;

/// <summary>
/// Atrapa todas las excepciones en un solo lugar y las convierte en respuestas HTTP claras.
/// Así los controladores no necesitan try/catch.
///   ValidacionException (datos incorrectos) → 400 con la lista de errores
///   CredencialesInvalidasException          → 401 con mensaje genérico
///   DomainException (regla de negocio)      → 422 con el mensaje
///   Cualquier otra                          → 500 sin mostrar detalles internos
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is ValidacionException ve)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación",
                Detail = ve.Message
            };
            problem.Extensions["errores"] = ve.Errores;
        }
        else if (exception is CredencialesInvalidasException ce)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "No autorizado",
                Detail = ce.Message
            };
        }
        else if (exception is DomainException de)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Regla de negocio no cumplida",
                Detail = de.Message
            };
        }
        else
        {
            logger.LogError(exception, "Error no controlado");
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocurrió un error inesperado"
            };
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
