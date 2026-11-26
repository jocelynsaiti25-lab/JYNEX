
using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Registro en la bitácora de inventario (ENTRADA, SALIDA o BAJA).
/// Tabla: movimiento.
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
    /// Crea un movimiento de ENTRADA inicial.
    /// HU-06 escenarios 1, 2 y 3.
    /// </summary>
    public static Movimiento CrearEntradaInicial(
        int idMedicamento,
        int idLote,
        int idUsuario,
        int cantidad,
        DateTime fechaUtc) =>
        CrearEntrada(idMedicamento, idLote, idUsuario, cantidad,
            "Alta inicial de medicamento", fechaUtc);

    /// <summary>
    /// Crea un movimiento de ENTRADA al registrar un nuevo lote.
    /// HU-07 escenario 10.
    /// </summary>
    public static Movimiento CrearEntradaLote(
        int idMedicamento,
        int idLote,
        int idUsuario,
        int cantidad,
        DateTime fechaUtc) =>
        CrearEntrada(idMedicamento, idLote, idUsuario, cantidad,
            "Entrada de nuevo lote", fechaUtc);

    /// <summary>
    /// Crea un movimiento de SALIDA con los lotes utilizados.
    /// HU-13.
    /// </summary>
    public static Movimiento CrearSalida(
        int idMedicamento,
        int idUsuario,
        IReadOnlyCollection<(int IdLote, int Cantidad)> lotes,
        DateTime fechaUtc)
    {
        if (idMedicamento <= 0)
            throw new DomainException("Debe seleccionar un medicamento válido.");

        if (idUsuario <= 0)
            throw new DomainException("El usuario que realiza la salida no es válido.");

        if (lotes is null || lotes.Count == 0)
            throw new DomainException("Debe existir al menos un lote para registrar la salida.");

        if (lotes.Any(l => l.IdLote <= 0 || l.Cantidad <= 0))
            throw new DomainException("Los lotes y sus cantidades deben ser válidos.");

        var movimiento = new Movimiento
        {
            Tipo = TipoSalida,
            IdMedicamento = idMedicamento,
            IdUsuario = idUsuario,
            CantidadTotal = lotes.Sum(l => l.Cantidad),
            Motivo = "Salida de medicamentos",
            Fecha = fechaUtc
        };

        foreach (var lote in lotes)
        {
            movimiento._detalles.Add(
                DetalleMovimiento.Crear(
                    idMedicamento,
                    lote.IdLote,
                    lote.Cantidad));
        }

        return movimiento;
    }

    private static Movimiento CrearEntrada(
        int idMedicamento,
        int idLote,
        int idUsuario,
        int cantidad,
        string motivo,
        DateTime fechaUtc)
    {
        if (cantidad <= 0)
            throw new DomainException(
                "Un movimiento de entrada debe tener una cantidad mayor a 0.");

        var movimiento = new Movimiento
        {
            Tipo = TipoEntrada,
            IdMedicamento = idMedicamento,
            IdUsuario = idUsuario,
            CantidadTotal = cantidad,
            Motivo = motivo,
            Fecha = fechaUtc
        };

        movimiento._detalles.Add(
            DetalleMovimiento.Crear(idMedicamento, idLote, cantidad));

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
