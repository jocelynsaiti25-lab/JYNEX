using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Registro en la bitácora de inventario (ENTRADA, SALIDA o BAJA). Tabla: movimiento.
/// </summary>
public class Movimiento : Entity<int>
{
    public const string TipoEntrada = "ENTRADA";
    public const string TipoSalida = "SALIDA";
    public const string TipoBaja = "BAJA";

    public string Tipo { get; private set; } = string.Empty;
    public int IdMedicamento { get; private set; }
    public int IdUsuario { get; private set; }
    public int CantidadTotal { get; private set; }
    public string? Motivo { get; private set; }
    public DateTime Fecha { get; private set; }

    private readonly List<DetalleMovimiento> _detalles = [];
    public IReadOnlyCollection<DetalleMovimiento> Detalles => _detalles.AsReadOnly();

    private Movimiento() { }

    /// <summary>
    /// Crea un movimiento de ENTRADA inicial al registrar un medicamento con unidades > 0.
    /// HU-06 escenarios 1, 2 y 3.
    /// </summary>
    public static Movimiento CrearEntradaInicial(
        int idMedicamento,
        int idLote,
        int idUsuario,
        int cantidad,
        DateTime fechaUtc)
    {
        if (cantidad <= 0)
            throw new DomainException("Un movimiento de entrada debe tener una cantidad mayor a 0.");

        var movimiento = new Movimiento
        {
            Tipo = TipoEntrada,
            IdMedicamento = idMedicamento,
            IdUsuario = idUsuario,
            CantidadTotal = cantidad,
            Motivo = "Alta inicial de medicamento",
            Fecha = fechaUtc
        };

        movimiento._detalles.Add(DetalleMovimiento.Crear(idMedicamento, idLote, cantidad));
        return movimiento;
    }

    /// <summary>
    /// Crea un movimiento de ENTRADA en bitácora al registrar la llegada de un nuevo lote.
    /// HU-07 escenario 10.
    /// </summary>
    public static Movimiento CrearEntradaLote(
        int idMedicamento,
        int idLote,
        int idUsuario,
        int cantidad,
        DateTime fechaUtc)
    {
        if (cantidad <= 0)
            throw new DomainException("Un movimiento de entrada debe tener una cantidad mayor a 0.");

        var movimiento = new Movimiento
        {
            Tipo = TipoEntrada,
            IdMedicamento = idMedicamento,
            IdUsuario = idUsuario,
            CantidadTotal = cantidad,
            Motivo = "Entrada de nuevo lote",
            Fecha = fechaUtc
        };

        movimiento._detalles.Add(DetalleMovimiento.Crear(idMedicamento, idLote, cantidad));
        return movimiento;
    }

    public void AsignarId(int id)
    {
        Id = id;
        foreach (var detalle in _detalles)
        {
            detalle.AsignarIdMovimiento(id);
        }
    }
}