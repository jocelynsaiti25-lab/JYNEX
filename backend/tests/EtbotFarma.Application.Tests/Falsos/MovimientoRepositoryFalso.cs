using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class MovimientoRepositoryFalso : IMovimientoRepository
{
    public List<Movimiento> Movimientos { get; } = [];

    public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
    {
        Movimientos.Add(movimiento);
        return Task.FromResult(Movimientos.Count);
    }

    public List<MovimientoBitacoraDto> Bitacora { get; } = [];

    public List<MedicamentoBitacoraDto> MedicamentosDeBitacora { get; } = [];

    public FiltroBitacoraDto? UltimoFiltro { get; private set; }

    public int UltimaPagina { get; private set; }

    public int UltimoTamanoPagina { get; private set; }

    public Task<(IReadOnlyList<MovimientoBitacoraDto> Movimientos, int Total)> ConsultarAsync(
        FiltroBitacoraDto filtro,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        UltimoFiltro = filtro;
        UltimaPagina = pagina;
        UltimoTamanoPagina = tamanoPagina;

        var coincidencias = Bitacora
            .Where(m => (filtro.DesdeUtc is null || m.FechaUtc >= filtro.DesdeUtc)
                && (filtro.HastaUtcExclusivo is null || m.FechaUtc < filtro.HastaUtcExclusivo)
                && (filtro.IdUsuario is null || m.IdUsuario == filtro.IdUsuario)
                && (filtro.IdMedicamento is null || m.IdMedicamento == filtro.IdMedicamento)
                && (filtro.Tipo is null || m.Tipo == filtro.Tipo))
            .OrderByDescending(m => m.FechaUtc)
            .ThenByDescending(m => m.IdMovimiento)
            .ToList();

        IReadOnlyList<MovimientoBitacoraDto> paginaActual = coincidencias
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return Task.FromResult((paginaActual, coincidencias.Count));
    }

    public Task<IReadOnlyList<MedicamentoBitacoraDto>> ListarMedicamentosAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MedicamentoBitacoraDto>>(MedicamentosDeBitacora.ToList());
}
