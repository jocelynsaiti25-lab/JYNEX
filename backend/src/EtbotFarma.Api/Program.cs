using EtbotFarma.Api.Middleware;
using EtbotFarma.Api.Seguridad;
using EtbotFarma.Application;
using EtbotFarma.Infrastructure;
using EtbotFarma.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace EtbotFarma.Api;

/// <summary>
/// Punto de entrada de la API. Aquí empieza a ejecutarse el backend.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuración local de cada integrante (cadena de conexión, contraseña del admin).
        // Este archivo NO se sube a GitHub.
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        // ---------- 1. Registrar servicios ----------
        builder.Services
            .AddApplication()                              // CQRS (commands y queries)
            .AddInfrastructure(builder.Configuration);     // BD, seguridad, servicios técnicos

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();              // Errores con formato estándar (RFC 7807)
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddOpenApi();                     // Documento OpenAPI en /openapi/v1.json

        // HU-01: autenticación con la cookie de sesión
        builder.Services
            .AddAuthentication(SesionAuthenticationHandler.Esquema)
            .AddScheme<AuthenticationSchemeOptions, SesionAuthenticationHandler>(SesionAuthenticationHandler.Esquema, null);

        // HU-01 criterio 5: TODO está protegido por defecto.
        // Lo público se marca con [AllowAnonymous] (por ejemplo, iniciar sesión).
        builder.Services.AddAuthorization(opciones =>
        {
            opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        var app = builder.Build();

        // ---------- 2. Configurar el pipeline HTTP ----------
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // ---------- 3. Preparar la base de datos (administrador inicial) ----------
        try
        {
            await app.Services.InicializarBaseDatosAsync();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "No se pudo conectar con la base de datos. Revisa appsettings.Local.json.");
        }

        // ---------- 4. Iniciar la aplicación ----------
        await app.RunAsync();
    }
}
