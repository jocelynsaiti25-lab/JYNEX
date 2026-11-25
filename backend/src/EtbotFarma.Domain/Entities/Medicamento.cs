using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Representa un medicamento del catálogo. Tabla: medicamento.
/// El stock NO se almacena aquí; vive en cada Lote.
/// </summary>
public class Medicamento : Entity<int>
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

    /// <summary>
    /// Crea un medicamento nuevo para el catálogo.
    /// HU-06 criterios 1, 15, 16 y regla de negocio (stock_minimo inicia en 0 y activo en true).
    /// </summary>
    public static Medicamento Crear(
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        bool esControlado,
        DateTime fechaRegistro)
    {
        if (string.IsNullOrWhiteSpace(nombreComercial))
            throw new DomainException("El nombre comercial es obligatorio.");

        if (string.IsNullOrWhiteSpace(nombreSustancia))
            throw new DomainException("La sustancia activa es obligatoria.");

        if (string.IsNullOrWhiteSpace(presentacion))
            throw new DomainException("La presentación es obligatoria.");

        if (string.IsNullOrWhiteSpace(concentracion))
            throw new DomainException("La concentración es obligatoria.");

        return new Medicamento
        {
            NombreComercial = nombreComercial.Trim(),
            NombreSustancia = nombreSustancia.Trim(),
            Presentacion = presentacion.Trim(),
            Concentracion = concentracion.Trim(),
            EsControlado = esControlado,
            StockMinimo = 0, // Regla de negocio US06: al registrar queda en 0 (se configura en HU-11)
            Activo = true,
            FechaRegistro = fechaRegistro
        };
    }

    /// <summary>Asigna el identificador generado por MySQL tras el INSERT.</summary>
    public void AsignarId(int id) => Id = id;

    /// <summary>Reconstruye la entidad desde una fila de MySQL sin ejecutar reglas de creación.</summary>
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