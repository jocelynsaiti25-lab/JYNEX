using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Enums;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un lote físico de un medicamento.
/// </summary>
public sealed class Lote : Entity<int>
{
    public int IdMedicamento { get; private set; }
    public string NumeroLote { get; private set; } = string.Empty;
    public DateOnly FechaCaducidad { get; private set; }
    public int Stock { get; private set; }
    public DateOnly FechaEntrada { get; private set; }

    private Lote() { }

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

    /// <summary>
    /// HU-08 escenario 3: calcula el semáforo de caducidad en meses calendario
    /// (Verde > 12 meses, Amarillo > 3 y <= 12 meses, Rojo <= 3 meses o caducado).
    /// </summary>
    public SemaforoCaducidad ObtenerSemaforo(DateOnly fechaHoy)
    {
        if (FechaCaducidad <= fechaHoy)
            return SemaforoCaducidad.Rojo;

        if (FechaCaducidad <= fechaHoy.AddMonths(3))
            return SemaforoCaducidad.Rojo;

        if (FechaCaducidad <= fechaHoy.AddMonths(12))
            return SemaforoCaducidad.Amarillo;

        return SemaforoCaducidad.Verde;
    }
}