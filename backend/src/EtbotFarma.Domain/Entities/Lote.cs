using System.Text.RegularExpressions;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Enums;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Lote específico de un medicamento con su fecha de caducidad y existencias. Tabla: lote.
/// </summary>
public class Lote : Entity<int>
{
    private static readonly Regex PatronNumeroLote = new("^[a-zA-Z0-9-]+$", RegexOptions.Compiled);

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

        // HU-06 escenario 13: solo letras, números y guiones, máximo 50 caracteres
        if (string.IsNullOrWhiteSpace(loteNormalizado)
            || loteNormalizado.Length > 50
            || !PatronNumeroLote.IsMatch(loteNormalizado))
        {
            throw new DomainException("El número de lote solo puede tener letras, números y guiones.");
        }

        // HU-06 escenario 12: la cantidad debe ser 0 o mayor
        if (cantidadInicial < 0)
        {
            throw new DomainException("La cantidad debe ser un número entero de 0 o más.");
        }

        // HU-06 escenario 10: no se permite registrar un lote caducado (hoy o anterior)
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
    /// Crea un nuevo lote para reabastecer un medicamento existente (HU-07 escenarios 3, 4 y 5).
    /// A diferencia del alta inicial, la cantidad debe ser estrictamente mayor a 0.
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

    public void AsignarId(int id) => Id = id;

    /// <summary>
    /// Calcula el color del semáforo de caducidad en meses calendario (HU-06 escenarios 11 y 20).
    /// Rojo: <= 3 meses · Amarillo: > 3 y <= 12 meses · Verde: > 12 meses.
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
    /// Indica si el lote ya venció (HU-10 escenario 5): su fecha de caducidad es igual o anterior a hoy.
    /// Un lote caducado siempre se clasifica en Rojo (ObtenerSemaforo), pero además lleva esta
    /// marca para que la capa de presentación muestre la etiqueta de alerta "CADUCADO".
    /// </summary>
    public bool EstaCaducado(DateOnly fechaHoy) => FechaCaducidad <= fechaHoy;

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