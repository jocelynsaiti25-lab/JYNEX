using System.Reflection;
using EtbotFarma.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Application;

/// <summary>
/// Registra todos los servicios de la capa Application.
/// Se llama desde Program.cs con: builder.Services.AddApplication();
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // CQRS: busca automáticamente todos los handlers (commands y queries) de este proyecto
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Antes de ejecutar cualquier handler, se ejecutan sus validadores
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidation: busca automáticamente todas las clases XxxValidator
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
