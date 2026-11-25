using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Entidad de dominio que representa un medicamento del catálogo de la farmacia.
/// </summary>
public sealed class Medicamento : Entity<int>
{
    public string NombreComercial { get; private set; } = string.Empty;
    public string NombreSustancia { get; private set; } = string.Empty;
    public string Presentacion { get; private set; } = string.Empty;
    public string Concentracion { get; private set; } = string.Empty;
    public bool EsControlado { get; private set; }
    public int StockMinimo { get; private set; }
    public bool Activo { get; private set; }
    public DateTime FechaRegistro { get; private set; }

    private Medicamento() { }

    public static Medicamento Crear(
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        bool esControlado,
        int stockMinimo,
        DateTime fechaRegistroUtc)
    {
        if (string.IsNullOrWhiteSpace(nombreComercial))
            throw new DomainException("El nombre comercial del medicamento es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombreSustancia))
            throw new DomainException("La sustancia activa del medicamento es obligatoria.");
        if (string.IsNullOrWhiteSpace(presentacion))
            throw new DomainException("La presentación del medicamento es obligatoria.");
        if (string.IsNullOrWhiteSpace(concentracion))
            throw new DomainException("La concentración del medicamento es obligatoria.");
        if (stockMinimo < 0)
            throw new DomainException("El stock mínimo no puede ser negativo.");

        return new Medicamento
        {
            NombreComercial = nombreComercial.Trim(),
            NombreSustancia = nombreSustancia.Trim(),
            Presentacion = presentacion.Trim(),
            Concentracion = concentracion.Trim(),
            EsControlado = esControlado,
            StockMinimo = stockMinimo,
            Activo = true,
            FechaRegistro = fechaRegistroUtc
        };
    }

    public static Medicamento Reconstruir(
        int id,
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        bool esControlado,
        int stockMinimo,
        bool activo,
        DateTime fechaRegistro)
    {
        return new Medicamento
        {
            Id = id,
            NombreComercial = nombreComercial,
            NombreSustancia = nombreSustancia,
            Presentacion = presentacion,
            Concentracion = concentracion,
            EsControlado = esControlado,
            StockMinimo = stockMinimo,
            Activo = activo,
            FechaRegistro = fechaRegistro
        };
    }
}