using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Infrastructure;

/// <summary>
/// Registra las implementaciones técnicas.
/// Se llama desde Program.cs con: builder.Services.AddInfrastructure(builder.Configuration);
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // Pendiente (HU-01): registrar el DbContext con la cadena de conexión
        // var connectionString = configuration.GetConnectionString("EtbotFarma");
        // services.AddDbContext<EtbotFarmaDbContext>(o => o.UseSqlServer(connectionString));

        return services;
    }
}
