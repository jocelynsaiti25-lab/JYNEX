using EtbotFarma.Domain.Entities;
using EtbotFarma.Application.BitacoraMovimientos.Dtos;

namespace EtbotFarma.Application.Common.Interfaces.Repositorios;

/// <summary>Contrato de persistencia para la bitácora de movimientos y sus detalles.</summary>
public interface IMovimientoRepository
{
    /// <summary>Inserta el movimiento y sus filas en detalle_movimiento dentro de la transacción activa.</summary>
    Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken);

    /// <summary>Consulta movimientos completos con sus detalles, aplicando filtros y paginación por movimiento.</summary>
    Task<PaginaBitacoraDto> ConsultarAsync(FiltroBitacoraDto filtro, CancellationToken cancellationToken);

    /// <summary>Lista medicamentos que tienen movimientos, incluidos los que están inactivos.</summary>
    Task<IReadOnlyList<MedicamentoBitacoraDto>> ConsultarMedicamentosAsync(CancellationToken cancellationToken);
}
