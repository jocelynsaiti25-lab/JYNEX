using EtbotFarma.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EtbotFarma.Api.IntegrationTests;

/// <summary>
/// Levanta la API completa en memoria (sin abrir puertos) conectada a MySQL.
/// Antes de las pruebas crea el administrador inicial, igual que Program.cs al arrancar.
/// </summary>
public sealed class ApiConMySql : WebApplicationFactory<Program>, IAsyncLifetime
{
    public async Task InitializeAsync() => await Services.InicializarBaseDatosAsync();

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Cliente HTTP de prueba. Usa https porque la cookie de sesión es "Secure".
    /// No guarda cookies solo: las pruebas las mandan a mano para revisarlas.
    /// </summary>
    public HttpClient CrearCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false
    });
}
