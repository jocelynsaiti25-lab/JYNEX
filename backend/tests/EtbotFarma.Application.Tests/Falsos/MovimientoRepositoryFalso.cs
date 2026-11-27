using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class MovimientoRepositoryFalso : IMovimientoRepository
{
    public List<Movimiento> Movimientos { get; } = [];
    public IReadOnlyList<MedicamentoBitacoraDto> MedicamentosBitacora { get; set; } = [];
    public FiltroBitacoraDto? FiltroConsultado { get; private set; }

    public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
    {
        Movimientos.Add(movimiento);
        return Task.FromResult(Movimientos.Count);
    }

    public Task<PaginaBitacoraDto> ConsultarAsync(FiltroBitacoraDto filtro, CancellationToken cancellationToken)
    {
        FiltroConsultado = filtro;
        return Task.FromResult(new PaginaBitacoraDto([], filtro.Pagina, filtro.TamanoPagina, 0, 0));
    }

    public Task<IReadOnlyList<MedicamentoBitacoraDto>> ConsultarMedicamentosAsync(CancellationToken cancellationToken)
        => Task.FromResult(MedicamentosBitacora);
}
