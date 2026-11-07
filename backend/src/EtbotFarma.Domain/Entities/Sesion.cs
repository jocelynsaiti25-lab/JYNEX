using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Un inicio de sesión de un usuario. Tabla: sesion.
/// TokenHash guarda el HASH del token de la cookie, nunca el token original.
/// La sesión vence en FechaExpiracion; cada actividad la recorre (HU-01 criterio 6).
/// </summary>
public class Sesion : Entity<int>
{
    public int IdUsuario { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime FechaInicio { get; private set; }
    public DateTime UltimaActividad { get; private set; }
    public DateTime FechaExpiracion { get; private set; }
    public DateTime? FechaCierre { get; private set; }

    /// <summary>Está cerrada si tiene fecha de cierre (no hace falta otra columna).</summary>
    public bool Cerrada => FechaCierre is not null;

    private Sesion() { }

    /// <summary>Abre una sesión nueva que vence después del tiempo de inactividad permitido.</summary>
    public static Sesion Iniciar(Usuario usuario, string tokenHash, DateTime ahora, TimeSpan inactividadMaxima)
    {
        if (!usuario.PuedeIniciarSesion())
        {
            throw new DomainException("La cuenta no está activa.");
        }

        return new Sesion
        {
            IdUsuario = usuario.Id,
            Usuario = usuario,
            TokenHash = tokenHash,
            FechaInicio = ahora,
            UltimaActividad = ahora,
            FechaExpiracion = ahora + inactividadMaxima
        };
    }

    /// <summary>
    /// Arma una sesión con los datos leídos de MySQL (lo usa SesionRepository).
    /// No aplica reglas: los datos ya estaban guardados.
    /// </summary>
    public static Sesion Reconstruir(
        int id,
        Usuario usuario,
        string tokenHash,
        DateTime fechaInicio,
        DateTime ultimaActividad,
        DateTime fechaExpiracion,
        DateTime? fechaCierre)
    {
        return new Sesion
        {
            Id = id,
            IdUsuario = usuario.Id,
            Usuario = usuario,
            TokenHash = tokenHash,
            FechaInicio = fechaInicio,
            UltimaActividad = ultimaActividad,
            FechaExpiracion = fechaExpiracion,
            FechaCierre = fechaCierre
        };
    }

    /// <summary>
    /// Regla de negocio (HU-01 criterio 6): la sesión sigue vigente si no está cerrada
    /// y todavía no llega su fecha de expiración.
    /// </summary>
    public bool EstaVigente(DateTime ahora) => !Cerrada && ahora <= FechaExpiracion;

    /// <summary>Cada petición del usuario recorre la expiración (reinicia el contador de inactividad).</summary>
    public void RegistrarActividad(DateTime ahora, TimeSpan inactividadMaxima)
    {
        if (Cerrada)
        {
            throw new DomainException("La sesión ya está cerrada.");
        }

        UltimaActividad = ahora;
        FechaExpiracion = ahora + inactividadMaxima;
    }

    public void Cerrar(DateTime ahora)
    {
        if (Cerrada)
        {
            return;
        }

        FechaCierre = ahora;
    }
}
