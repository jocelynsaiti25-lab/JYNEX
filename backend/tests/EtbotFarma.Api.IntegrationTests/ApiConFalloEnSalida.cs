using EtbotFarma.Application.BitacoraMovimientos.Dtos;
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

        public Task<(IReadOnlyList<MovimientoBitacoraDto> Movimientos, int Total)> ConsultarAsync(
            FiltroBitacoraDto filtro, int pagina, int tamanoPagina, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MedicamentoBitacoraDto>> ListarMedicamentosAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
