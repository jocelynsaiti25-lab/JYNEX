using System.Reflection;
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
        // CQRS: busca automáticamente todos los handlers (commands y queries) de este proyecto
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
