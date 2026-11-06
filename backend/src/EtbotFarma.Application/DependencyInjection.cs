using System.Reflection;
using EtbotFarma.Application.Common.Behaviors;
using EtbotFarma.Application.Common.Validacion;
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

        // CQRS: busca automáticamente todos los handlers (carpeta Handlers)
        // y agrega el paso de validación antes de cada uno
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Validadores: busca automáticamente todas las clases que implementan IValidator<T>
        // (carpeta Validators). Al crear un validador nuevo NO hay que registrarlo a mano.
        RegistrarValidadores(services, assembly);

        return services;
    }

    private static void RegistrarValidadores(IServiceCollection services, Assembly assembly)
    {
        var clases = assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var clase in clases)
        {
            var interfaces = clase.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

            foreach (var interfaz in interfaces)
            {
                services.AddTransient(interfaz, clase);
            }
        }
    }
}
