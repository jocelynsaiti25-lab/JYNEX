using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para la bitácora de movimientos y sus detalles.</summary>
public interface IMovimientoRepository
{
    /// <summary>Inserta el movimiento y sus filas en detalle_movimiento dentro de la transacción activa.</summary>
    Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken);

    Task<(IReadOnlyList<MovimientoBitacoraDto> Movimientos, int Total)> ConsultarAsync(
        FiltroBitacoraDto filtro,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MedicamentoBitacoraDto>> ListarMedicamentosAsync(CancellationToken cancellationToken);
}
