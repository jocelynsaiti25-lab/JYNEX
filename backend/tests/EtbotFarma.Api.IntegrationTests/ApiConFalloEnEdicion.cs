using System.Reflection;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Infrastructure.Persistence;
using EtbotFarma.Infrastructure.Persistence.Repositorios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests;

public sealed class FalloSimuladoException(string mensaje) : Exception(mensaje);

public abstract class ApiConFalloBase : WebApplicationFactory<Program>, IAsyncLifetime
{
    public async Task InitializeAsync() => await Services.InicializarBaseDatosAsync();

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public HttpClient CrearCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false
    });

    protected static void Reemplazar<TServicio>(IServiceCollection servicios, Func<IServiceProvider, TServicio> fabrica)
        where TServicio : class
    {
        var existente = servicios.Single(d => d.ServiceType == typeof(TServicio));
        servicios.Remove(existente);
        servicios.AddScoped(fabrica);
    }
}

public sealed class ApiConAuditoriaQueFalla : ApiConFalloBase
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(servicios =>
            Reemplazar<IAuditoriaRepository>(servicios, _ => new AuditoriaQueFalla()));

    private sealed class AuditoriaQueFalla : IAuditoriaRepository
    {
        public Task AgregarAsync(Auditoria auditoria, CancellationToken cancellationToken)
            => throw new FalloSimuladoException("Falla simulada al guardar la auditoría");
    }
}

public class ProxyQueFallaTrasActualizar : DispatchProxy
{
    public IMedicamentoRepository Real { get; set; } = null!;

    protected override object? Invoke(MethodInfo? metodo, object?[]? argumentos)
    {
        var resultado = metodo!.Invoke(Real, argumentos);
        return metodo.Name == nameof(IMedicamentoRepository.ActualizarAsync)
            ? FallarDespuesDeAsync((Task)resultado!)
            : resultado;
    }

    private static async Task FallarDespuesDeAsync(Task escritura)
    {
        await escritura;
        throw new FalloSimuladoException("Falla simulada después de escribir la actualización");
    }
}

public sealed class ApiConActualizacionQueFalla : ApiConFalloBase
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(servicios =>
            Reemplazar<IMedicamentoRepository>(servicios, proveedor =>
            {
                var proxy = DispatchProxy.Create<IMedicamentoRepository, ProxyQueFallaTrasActualizar>();
                ((ProxyQueFallaTrasActualizar)(object)proxy).Real = ActivatorUtilities.CreateInstance<MedicamentoRepository>(proveedor);
                return proxy;
            }));
}
