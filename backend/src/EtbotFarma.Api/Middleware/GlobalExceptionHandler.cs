using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Middleware;

/// <summary>
/// Atrapa TODAS las excepciones en un solo lugar y las convierte en respuestas HTTP claras.
/// Por eso los controllers no necesitan try/catch.
///
///   ValidacionException            → 400  datos de entrada incorrectos (lista "errores")
///   CredencialesInvalidasException → 401  usuario o contraseña incorrectos (mensaje genérico)
///   NoAutenticadoException         → 401  no hay sesión válida
///   AccesoDenegadoException        → 403  el rol no tiene permiso
///   NoEncontradoException          → 404  el registro no existe
///   DomainException                → 422  regla de negocio no cumplida
///   Cualquier otra                 → 500  sin mostrar detalles internos (solo se registra en el log)
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
        var problem = exception switch
        {
            ValidacionException => Crear(StatusCodes.Status400BadRequest, "Error de validación", exception.Message),
            CredencialesInvalidasException => Crear(StatusCodes.Status401Unauthorized, "No autorizado", exception.Message),
            NoAutenticadoException => Crear(StatusCodes.Status401Unauthorized, "No autorizado", exception.Message),
            AccesoDenegadoException => Crear(StatusCodes.Status403Forbidden, "Acceso denegado", exception.Message),
            NoEncontradoException => Crear(StatusCodes.Status404NotFound, "No encontrado", exception.Message),
            DomainException => Crear(StatusCodes.Status422UnprocessableEntity, "Regla de negocio no cumplida", exception.Message),
            _ => Crear(StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado", null)
        };

        if (exception is ValidacionException validacion)
        {
            problem.Extensions["errores"] = validacion.Errores;
        }

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado");
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Crear(int estado, string titulo, string? detalle) => new()
    {
        Status = estado,
        Title = titulo,
        Detail = detalle
    };
}
