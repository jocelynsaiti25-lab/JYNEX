using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Persona que usa el sistema. Tabla: usuario.
/// Contiene sus propias reglas de negocio (por ejemplo, si puede iniciar sesión).
/// </summary>
public class Usuario : Entity<int>
{
    public string Nombre { get; private set; } = string.Empty;
    public string Apellidos { get; private set; } = string.Empty;
    public string NombreUsuario { get; private set; } = string.Empty;
    public string ContraseniaHash { get; private set; } = string.Empty;
    public string Telefono { get; private set; } = string.Empty;
    public int IdRol { get; private set; }
    public Rol Rol { get; private set; } = null!;
    public bool Activo { get; private set; }
    public DateTime FechaRegistro { get; private set; }

    public string NombreCompleto => $"{Nombre} {Apellidos}";

    // Constructor vacío que usa Entity Framework para leer de la base de datos
    private Usuario() { }

    /// <summary>Crea un usuario nuevo y activo.</summary>
    public static Usuario Crear(
        string nombre,
        string apellidos,
        string nombreUsuario,
        string contraseniaHash,
        string telefono,
        Rol rol,
        DateTime fechaRegistro)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new DomainException("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(contraseniaHash))
        {
            throw new DomainException("La contraseña es obligatoria.");
        }

        return new Usuario
        {
            Nombre = nombre.Trim(),
            Apellidos = apellidos.Trim(),
            NombreUsuario = nombreUsuario.Trim(),
            ContraseniaHash = contraseniaHash,
            Telefono = telefono.Trim(),
            IdRol = rol.Id,
            Rol = rol,
            Activo = true,
            FechaRegistro = fechaRegistro
        };
    }

    /// <summary>Regla de negocio (HU-01 criterio 2): solo una cuenta activa puede iniciar sesión.</summary>
    public bool PuedeIniciarSesion() => Activo;

    public void Desactivar() => Activo = false;
}
