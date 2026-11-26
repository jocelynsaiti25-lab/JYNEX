using System.Text.Encodings.Web;
using System.Text.Json;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.ValueObjects;

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

    /// <summary>
    /// HU-07 escenario 2: valida que el medicamento esté activo en el catálogo antes de recibir un nuevo lote.
    /// </summary>
    public void ValidarDisponibleParaEntrada()
    {
        if (!Activo)
            throw new DomainException("No se puede registrar una entrada de lote para un medicamento inactivo.");
    }

    /// <summary>
    /// HU-11 escenario 1: define o edita el umbral de stock mínimo del medicamento.
    /// </summary>
    public void ActualizarStockMinimo(int nuevoStockMinimo)
    {
        if (nuevoStockMinimo < 0)
            throw new DomainException("El stock mínimo debe ser un número entero de 0 o más.");

        StockMinimo = nuevoStockMinimo;
    }

    /// <summary>
    /// HU-09 escenarios 1 y 9: actualiza SOLO los datos descriptivos del catálogo.
    /// Conserva el Id, el stock mínimo, el estado activo y la fecha de registro (nunca se tocan aquí).
    /// </summary>
    public void Editar(
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        bool esControlado)
    {
        if (string.IsNullOrWhiteSpace(nombreComercial))
            throw new DomainException("El nombre comercial es obligatorio.");

        if (string.IsNullOrWhiteSpace(nombreSustancia))
            throw new DomainException("La sustancia activa es obligatoria.");

        if (string.IsNullOrWhiteSpace(presentacion))
            throw new DomainException("La presentación es obligatoria.");

        if (string.IsNullOrWhiteSpace(concentracion))
            throw new DomainException("La concentración es obligatoria.");

        NombreComercial = nombreComercial.Trim();
        NombreSustancia = nombreSustancia.Trim();
        Presentacion = presentacion.Trim();
        Concentracion = concentracion.Trim();
        EsControlado = esControlado;
    }

    /// <summary>
    /// HU-09 escenario 9: compara contra los valores ACTUALES para saber si el formulario trae algún cambio real
    /// (sin importar mayúsculas ni espacios) antes de aplicar Editar.
    /// </summary>
    public bool TieneCambiosDescriptivos(
        string nombreComercial,
        string nombreSustancia,
        string presentacion,
        string concentracion,
        bool esControlado)
    {
        return !NombreComercial.Equals(nombreComercial.Trim(), StringComparison.Ordinal)
            || !NombreSustancia.Equals(nombreSustancia.Trim(), StringComparison.Ordinal)
            || !Presentacion.Equals(presentacion.Trim(), StringComparison.Ordinal)
            || !Concentracion.Equals(concentracion.Trim(), StringComparison.Ordinal)
            || EsControlado != esControlado;
    }

    /// <summary>HU-09 escenario 1: datos descriptivos para la auditoría (antes/después), sin stock ni lotes.</summary>
    public string DatosParaAuditoria() => JsonSerializer.Serialize(new
    {
        nombreComercial = NombreComercial,
        nombreSustancia = NombreSustancia,
        presentacion = Presentacion,
        concentracion = Concentracion,
        esControlado = EsControlado
    }, OpcionesJson);

    /// <summary>Guarda los acentos legibles (sin escapes Unicode) para que se lea fácil en MySQL.</summary>
    private static readonly JsonSerializerOptions OpcionesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// HU-11: solo cuenta como existencias las unidades disponibles y VIGENTES (con stock y sin caducar);
    /// un lote caducado nunca cuenta, aunque todavía tenga unidades (Lote.EstaCaducado).
    /// </summary>
    public ExistenciasMedicamento CalcularExistencias(IEnumerable<Lote> lotes, DateOnly hoy)
    {
        var lotesVigentes = lotes.Where(l => l.Stock > 0 && !l.EstaCaducado(hoy)).ToList();
        var total = lotesVigentes.Sum(l => l.Stock);
        var estaAgotado = total == 0;
        var enStockBajo = !estaAgotado && StockMinimo > 0 && total <= StockMinimo;
        return new ExistenciasMedicamento(total, CalcularSemaforoGeneral(lotesVigentes, hoy), estaAgotado, enStockBajo);
    }

    private static string CalcularSemaforoGeneral(IReadOnlyCollection<Lote> lotesConStock, DateOnly hoy)
    {
        if (lotesConStock.Count == 0) return "SinStock";

        var peor = lotesConStock.Max(l => l.ObtenerSemaforo(hoy));
        return peor.ToString();
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