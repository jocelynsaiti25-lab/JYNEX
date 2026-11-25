using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Detalle de qué lote y qué cantidad participó en un movimiento. Tabla: detalle_movimiento.
/// </summary>
public class DetalleMovimiento : Entity<int>
{
    public int IdMovimiento { get; private set; }
    public int IdMedicamento { get; private set; }
    public int IdLote { get; private set; }
    public int Cantidad { get; private set; }

    private DetalleMovimiento() { }

    public static DetalleMovimiento Crear(int idMedicamento, int idLote, int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad del detalle de movimiento debe ser mayor a 0.");

        return new DetalleMovimiento
        {
            IdMedicamento = idMedicamento,
            IdLote = idLote,
            Cantidad = cantidad
        };
    }

    public void AsignarIdMovimiento(int idMovimiento) => IdMovimiento = idMovimiento;
}