using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Un inicio de sesión de un usuario. Tabla: sesion.
/// Token guarda el HASH del token de la cookie, nunca el token original.
/// </summary>
public class Sesion : Entity<int>
{
    public int IdUsuario { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public string Token { get; private set; } = string.Empty;
    public DateTime FechaInicio { get; private set; }
    public DateTime UltimaActividad { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public bool Cerrada { get; private set; }

    // Constructor vacío que usa Entity Framework para leer de la base de datos
    private Sesion() { }

    /// <summary>Abre una sesión nueva para el usuario.</summary>
    public static Sesion Iniciar(Usuario usuario, string tokenHash, DateTime ahora)
    {
        if (!usuario.PuedeIniciarSesion())
        {
            throw new DomainException("La cuenta no está activa.");
        }

        return new Sesion
        {
            IdUsuario = usuario.Id,
            Usuario = usuario,
            Token = tokenHash,
            FechaInicio = ahora,
            UltimaActividad = ahora,
            Cerrada = false
        };
    }

    /// <summary>
    /// Regla de negocio (HU-01 criterio 6): la sesión sigue vigente si no está cerrada
    /// y no ha pasado más tiempo de inactividad del permitido.
    /// </summary>
    public bool EstaVigente(DateTime ahora, TimeSpan inactividadMaxima)
    {
        if (Cerrada)
        {
            return false;
        }

        var tiempoSinActividad = ahora - UltimaActividad;
        return tiempoSinActividad <= inactividadMaxima;
    }

    /// <summary>Cada petición del usuario reinicia el contador de inactividad.</summary>
    public void RegistrarActividad(DateTime ahora)
    {
        if (Cerrada)
        {
            throw new DomainException("La sesión ya está cerrada.");
        }

        UltimaActividad = ahora;
    }

    public void Cerrar(DateTime ahora)
    {
        if (Cerrada)
        {
            return;
        }

        Cerrada = true;
        FechaCierre = ahora;
    }
}
