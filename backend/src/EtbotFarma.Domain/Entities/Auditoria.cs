using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Registro de un cambio administrativo: quién lo hizo, qué hizo, sobre qué registro y cuándo. Tabla: auditoria.
/// Se reutiliza en cualquier historia que cambie datos importantes
/// (HU-03 crear usuario, HU-05 reasignar contraseña, desactivar un usuario, editar un medicamento...).
///
/// REGLA (CLAUDE.md §7): nunca se guardan contraseñas, hashes ni tokens en ValorAnterior / ValorNuevo.
/// La tabla no tiene columna de "resultado": solo se guarda lo que SÍ se hizo, porque el registro
/// va en la misma transacción que el cambio (si el cambio falla, tampoco queda la auditoría).
/// </summary>
public class Auditoria : Entity<int>
{
    /// <summary>Acciones registradas (columna accion, máximo 50 caracteres).</summary>
    public const string CrearUsuario = "CREAR USUARIO";

    /// <summary>Nombres reales de las tablas (columna tabla_afectada).</summary>
    public const string TablaUsuario = "usuario";

    public int IdUsuario { get; private set; }
    public string Accion { get; private set; } = string.Empty;
    public string TablaAfectada { get; private set; } = string.Empty;
    public int IdRegistro { get; private set; }
    public string? ValorAnterior { get; private set; }
    public string? ValorNuevo { get; private set; }
    public DateTime Fecha { get; private set; }

    private Auditoria() { }

    /// <summary>
    /// Crea el registro de auditoría de un cambio.
    /// idUsuario: quién hizo el cambio (el usuario con sesión). idRegistro: el registro afectado.
    /// </summary>
    public static Auditoria Crear(
        int idUsuario,
        string accion,
        string tablaAfectada,
        int idRegistro,
        string? valorAnterior,
        string? valorNuevo,
        DateTime fecha)
    {
        if (idUsuario <= 0)
        {
            throw new DomainException("La auditoría necesita saber qué usuario hizo el cambio.");
        }

        if (string.IsNullOrWhiteSpace(accion) || accion.Length > 50)
        {
            throw new DomainException("La acción de auditoría es obligatoria (máximo 50 caracteres).");
        }

        if (string.IsNullOrWhiteSpace(tablaAfectada) || tablaAfectada.Length > 50)
        {
            throw new DomainException("La tabla afectada es obligatoria (máximo 50 caracteres).");
        }

        if (idRegistro <= 0)
        {
            throw new DomainException("La auditoría necesita el id del registro afectado.");
        }

        return new Auditoria
        {
            IdUsuario = idUsuario,
            Accion = accion,
            TablaAfectada = tablaAfectada,
            IdRegistro = idRegistro,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo,
            Fecha = fecha
        };
    }
}
