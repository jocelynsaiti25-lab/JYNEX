using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Infrastructure.Persistence;
using EtbotFarma.Infrastructure.Persistence.Repositorios;
using EtbotFarma.Infrastructure.Seguridad;
using EtbotFarma.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Infrastructure;

/// <summary>
/// Registra las implementaciones técnicas: cada interfaz de Application con su clase real.
/// Se llama desde Program.cs con: builder.Services.AddInfrastructure(builder.Configuration);
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------- Base de datos (MySQL con SQL a mano, sin ORM) ----------
        // Una conexión + transacción por petición. También es el IUnitOfWork (COMMIT).
        var cadenaConexion = configuration.GetConnectionString("EtbotFarma") ?? string.Empty;
        services.AddScoped(_ => new ConexionMySql(cadenaConexion));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ConexionMySql>());

        // ---------- Repositorios ----------
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ISesionRepository, SesionRepository>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();   // compartido: cambios administrativos

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
