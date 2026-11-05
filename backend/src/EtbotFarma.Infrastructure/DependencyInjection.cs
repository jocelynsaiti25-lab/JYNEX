using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Infrastructure.Persistence;
using EtbotFarma.Infrastructure.Persistence.Repositorios;
using EtbotFarma.Infrastructure.Seguridad;
using EtbotFarma.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Infrastructure;

/// <summary>
/// Registra las implementaciones técnicas: cada interfaz de Application con su clase real.
/// Se llama desde Program.cs con: builder.Services.AddInfrastructure(builder.Configuration);
/// </summary>
public static class DependencyInjection
{
    // Versión del servidor MySQL (se fija para no abrir una conexión al arrancar)
    private static readonly MySqlServerVersion VersionMySql = new(new Version(8, 0, 36));

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------- Base de datos ----------
        var cadenaConexion = configuration.GetConnectionString("EtbotFarma") ?? string.Empty;
        services.AddDbContext<EtbotFarmaDbContext>(opciones => opciones.UseMySql(cadenaConexion, VersionMySql));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EtbotFarmaDbContext>());

        // ---------- Repositorios ----------
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ISesionRepository, SesionRepository>();

        // ---------- Seguridad ----------
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenSesionService, TokenSesionService>();

        // ---------- Otros servicios ----------
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // ---------- Configuración (appsettings.json, sección "Sesion") ----------
        var configuracionSesion = configuration.GetSection("Sesion").Get<ConfiguracionSesion>() ?? new ConfiguracionSesion();
        services.AddSingleton(configuracionSesion);

        return services;
    }
}
