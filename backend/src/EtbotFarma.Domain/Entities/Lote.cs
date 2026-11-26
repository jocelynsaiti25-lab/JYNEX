
using System.Text.RegularExpressions;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Enums;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Lote específico de un medicamento con su fecha de caducidad y existencias.
/// Tabla: lote.
/// </summary>
public class Lote : Entity<int>
{
    private static readonly Regex PatronNumeroLote =
        new("^[a-zA-Z0-9-]+$", RegexOptions.Compiled);

    public int IdMedicamento { get; private set; }
    public string NumeroLote { get; private set; } = string.Empty;
    public DateOnly FechaCaducidad { get; private set; }
    public int Stock { get; private set; }
    public DateOnly FechaEntrada { get; private set; }

    private Lote() { }

    /// <summary>
    /// Crea el lote inicial de un medicamento nuevo.
    /// HU-06 escenarios 2, 3, 10, 11, 12 y 13.
    /// </summary>
    public static Lote Crear(
        int idMedicamento,
        string numeroLote,
        DateOnly fechaCaducidad,
        int cantidadInicial,
        DateOnly fechaHoy)
    {
        var loteNormalizado = numeroLote?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(loteNormalizado)
            || loteNormalizado.Length > 50
            || !PatronNumeroLote.IsMatch(loteNormalizado))
        {
            throw new DomainException("El número de lote solo puede tener letras, números y guiones.");
        }

        if (cantidadInicial < 0)
        {
            throw new DomainException("La cantidad debe ser un número entero de 0 o más.");
        }

        if (fechaCaducidad <= fechaHoy)
        {
            throw new DomainException("No se puede registrar un lote caducado.");
        }

        return new Lote
        {
            IdMedicamento = idMedicamento,
            NumeroLote = loteNormalizado,
            FechaCaducidad = fechaCaducidad,
            Stock = cantidadInicial,
            FechaEntrada = fechaHoy
        };
    }

    /// <summary>
    /// Crea un nuevo lote para reabastecer un medicamento existente.
    /// HU-07 escenarios 3, 4 y 5.
    /// </summary>
    public static Lote CrearEntrada(
        int idMedicamento,
        string numeroLote,
        DateOnly fechaCaducidad,
        int cantidad,
        DateOnly fechaHoy)
    {
        if (idMedicamento <= 0)
            throw new DomainException("Debe seleccionar un medicamento válido.");

        if (cantidad <= 0)
            throw new DomainException("La cantidad de entrada debe ser un número entero mayor que cero.");

        return Crear(idMedicamento, numeroLote, fechaCaducidad, cantidad, fechaHoy);
    }

    /// <summary>
    /// Descuenta existencias del lote al registrar una salida.
    /// HU-13.
    /// </summary>
    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad a descontar debe ser mayor que cero.");

        if (cantidad > Stock)
            throw new DomainException("No hay existencias suficientes en el lote.");

        Stock -= cantidad;
    }

    public void AsignarId(int id) => Id = id;

    /// <summary>
    /// Calcula el color del semáforo de caducidad en meses calendario.
    /// HU-06.
    /// </summary>
    public SemaforoCaducidad ObtenerSemaforo(DateOnly fechaHoy)
    {
        if (FechaCaducidad <= fechaHoy.AddMonths(3))
            return SemaforoCaducidad.Rojo;

        if (FechaCaducidad <= fechaHoy.AddMonths(12))
            return SemaforoCaducidad.Amarillo;

        return SemaforoCaducidad.Verde;
    }

    /// <summary>
    /// Indica si el lote ya venció.
    /// </summary>
    public bool EstaCaducado(DateOnly fechaHoy) =>
        FechaCaducidad <= fechaHoy;

    public static Lote Reconstruir(
        int id,
        int idMedicamento,
        string numeroLote,
        DateOnly fechaCaducidad,
        int stock,
        DateOnly fechaEntrada)
    {
        return new Lote
        {
            Id = id,
            IdMedicamento = idMedicamento,
            NumeroLote = numeroLote,
            FechaCaducidad = fechaCaducidad,
            Stock = stock,
            FechaEntrada = fechaEntrada
        };
    }
}
