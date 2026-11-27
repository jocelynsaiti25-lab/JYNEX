using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace EtbotFarma.Api.IntegrationTests;

public sealed class ApiConBitacoraQueFalla : ApiConFalloBase
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(servicios =>
            Reemplazar<IMovimientoRepository>(servicios, _ => new BitacoraQueFalla()));

    private sealed class BitacoraQueFalla : IMovimientoRepository
    {
        public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
            => throw new FalloSimuladoException("Falla simulada al guardar la bitácora");
        public Task<PaginaBitacoraDto> ConsultarAsync(FiltroBitacoraDto filtro, CancellationToken cancellationToken)
            => Task.FromResult(new PaginaBitacoraDto([], filtro.Pagina, filtro.TamanoPagina, 0, 0));
        public Task<IReadOnlyList<MedicamentoBitacoraDto>> ConsultarMedicamentosAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
