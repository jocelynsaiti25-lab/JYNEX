using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.ValueObjects;

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
        DateTime fechaUtc) =>
        CrearEntrada(idMedicamento, idLote, idUsuario, cantidad, "Alta inicial de medicamento", fechaUtc);

    /// <summary>
    /// Crea un movimiento de ENTRADA en bitácora al registrar la llegada de un nuevo lote.
    /// HU-07 escenario 10.
    /// </summary>
    public static Movimiento CrearEntradaLote(
        int idMedicamento,
        int idLote,
        int idUsuario,
        int cantidad,
        DateTime fechaUtc) =>
        CrearEntrada(idMedicamento, idLote, idUsuario, cantidad, "Entrada de nuevo lote", fechaUtc);

    private static Movimiento CrearEntrada(
        int idMedicamento,
        int idLote,
        int idUsuario,
        int cantidad,
        string motivo,
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
            Motivo = motivo,
            Fecha = fechaUtc
        };

        movimiento._detalles.Add(DetalleMovimiento.Crear(idMedicamento, idLote, cantidad));
        return movimiento;
    }

    public static Movimiento CrearSalida(
        int idMedicamento,
        int idUsuario,
        IReadOnlyCollection<AsignacionLote> asignaciones,
        DateTime fechaUtc)
    {
        if (asignaciones.Count == 0 || asignaciones.Any(a => a.Cantidad <= 0))
            throw new DomainException("Una salida debe tener al menos un lote y cada cantidad debe ser mayor a 0.");

        var movimiento = new Movimiento
        {
            Tipo = TipoSalida,
            IdMedicamento = idMedicamento,
            IdUsuario = idUsuario,
            CantidadTotal = asignaciones.Sum(a => a.Cantidad),
            Motivo = "Salida de medicamento (FEFO)",
            Fecha = fechaUtc
        };

        foreach (var asignacion in asignaciones)
        {
            movimiento._detalles.Add(DetalleMovimiento.Crear(idMedicamento, asignacion.IdLote, asignacion.Cantidad));
        }

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