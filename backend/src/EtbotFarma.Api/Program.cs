using EtbotFarma.Api.Middleware;
using EtbotFarma.Application;
using EtbotFarma.Infrastructure;

namespace EtbotFarma.Api;

/// <summary>
/// Punto de entrada de la API. Aquí empieza a ejecutarse el backend.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---------- 1. Registrar servicios ----------
        builder.Services
            .AddApplication()                              // CQRS + FluentValidation
            .AddInfrastructure(builder.Configuration);     // BD, servicios técnicos

        builder.Services.AddControllers();
        builder.Services.AddAuthorization();               // La autenticación por cookie se agrega en HU-01
        builder.Services.AddProblemDetails();              // Errores con formato estándar (RFC 7807)
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddOpenApi();                     // Documento OpenAPI en /openapi/v1.json

        var app = builder.Build();

        // ---------- 2. Configurar el pipeline HTTP ----------
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // app.UseAuthentication();   // Se activa en HU-01
        app.UseAuthorization();

        app.MapControllers();

        // ---------- 3. Iniciar la aplicación ----------
        app.Run();
    }
}
