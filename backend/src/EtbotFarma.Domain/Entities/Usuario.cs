using System.Text.Encodings.Web;
using System.Text.Json;
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

    /// <summary>HU-03: identifica a la persona; no se repite. Se guarda en minúsculas.</summary>
    public string Correo { get; private set; } = string.Empty;

    public string ContraseniaHash { get; private set; } = string.Empty;
    public string Telefono { get; private set; } = string.Empty;
    public int IdRol { get; private set; }
    public Rol Rol { get; private set; } = null!;
    public bool Activo { get; private set; }
    public DateTime FechaRegistro { get; private set; }

    public string NombreCompleto => $"{Nombre} {Apellidos}";

    private Usuario() { }

    /// <summary>Crea un usuario nuevo y activo (HU-03 criterio 5).</summary>
    public static Usuario Crear(
        string nombre,
        string apellidos,
        string nombreUsuario,
        string correo,
        string contraseniaHash,
        string telefono,
        Rol rol,
        DateTime fechaRegistro)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            throw new DomainException("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(correo))
        {
            throw new DomainException("El correo electrónico es obligatorio.");
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
            Correo = NormalizarCorreo(correo),
            ContraseniaHash = contraseniaHash,
            Telefono = telefono.Trim(),
            IdRol = rol.Id,
            Rol = rol,
            Activo = true,
            FechaRegistro = fechaRegistro
        };
    }

    /// <summary>
    /// Arma un usuario con los datos leídos de MySQL (lo usan los repositorios).
    /// No aplica reglas: los datos ya estaban guardados.
    /// </summary>
    public static Usuario Reconstruir(
        int id,
        string nombre,
        string apellidos,
        string nombreUsuario,
        string correo,
        string contraseniaHash,
        string telefono,
        Rol rol,
        bool activo,
        DateTime fechaRegistro)
    {
        return new Usuario
        {
            Id = id,
            Nombre = nombre,
            Apellidos = apellidos,
            NombreUsuario = nombreUsuario,
            Correo = correo,
            ContraseniaHash = contraseniaHash,
            Telefono = telefono,
            IdRol = rol.Id,
            Rol = rol,
            Activo = activo,
            FechaRegistro = fechaRegistro
        };
    }

    /// <summary>Regla de negocio (HU-01 criterio 2): solo una cuenta activa puede iniciar sesión.</summary>
    public bool PuedeIniciarSesion() => Activo;

    public void Desactivar() => Activo = false;

    /// <summary>
    /// HU-03: el correo se compara sin importar mayúsculas ni espacios:
    /// " Ana.Lopez@Gmail.com " y "ana.lopez@gmail.com" son el mismo correo.
    /// </summary>
    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    /// <summary>
    /// Datos del usuario para guardar en auditoria.valor_nuevo / valor_anterior (texto JSON).
    /// Solo lo necesario para identificar la cuenta: NUNCA la contraseña ni su hash,
    /// y tampoco el teléfono (dato de contacto que no hace falta para auditar).
    /// </summary>
    public string DatosParaAuditoria() => JsonSerializer.Serialize(new
    {
        nombreUsuario = NombreUsuario,
        nombreCompleto = NombreCompleto,
        correo = Correo,
        rol = Rol.Nombre,
        activo = Activo
    }, OpcionesJson);

    /// <summary>Guarda los acentos tal cual ("López" y no "L\u00F3pez") para que se lea fácil en MySQL.</summary>
    private static readonly JsonSerializerOptions OpcionesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
