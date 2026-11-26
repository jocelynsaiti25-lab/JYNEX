using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace EtbotFarma.Api.IntegrationTests;

public sealed class ApiConBitacoraDeSalidaQueFalla : ApiConFalloBase
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(servicios =>
            Reemplazar<IMovimientoRepository>(servicios, _ => new MovimientoQueFalla()));

    private sealed class MovimientoQueFalla : IMovimientoRepository
    {
        public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
            => throw new FalloSimuladoException("Falla simulada al guardar el movimiento de salida");
    }
}
