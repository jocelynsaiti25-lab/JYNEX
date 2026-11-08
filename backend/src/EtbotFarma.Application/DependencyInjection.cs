using System.Reflection;
using EtbotFarma.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Application;

/// <summary>
/// Registra todos los servicios de la capa Application.
/// Se llama desde Program.cs con: builder.Services.AddApplication();
/// Al crear un command, query, handler o validator nuevo NO hay que registrarlo a mano.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // CQRS con MediatR: encuentra automáticamente todos los handlers (carpeta Handlers)
        // y agrega el paso de validación que se ejecuta antes de cada uno
        services.AddMediatR(configuracion =>
        {
            configuracion.RegisterServicesFromAssembly(assembly);
            configuracion.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation: encuentra automáticamente todos los validators (carpeta Validators)
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
