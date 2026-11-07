namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// Datos del usuario que hizo la petición actual (ya validado por la cookie de sesión).
/// Lo usarán también las historias que registran quién hizo cada movimiento (bitácora).
/// La implementación está en la capa Api (lee la identidad de la petición HTTP).
/// </summary>
public interface IUsuarioActual
{
    /// <summary>null si la petición no tiene una sesión válida.</summary>
    int? IdUsuario { get; }

    string NombreUsuario { get; }

    string NombreCompleto { get; }

    string Rol { get; }
}
