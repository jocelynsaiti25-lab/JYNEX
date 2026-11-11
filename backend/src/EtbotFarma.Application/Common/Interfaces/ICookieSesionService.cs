namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// Maneja la cookie segura del navegador donde viaja el token de la sesión.
/// Application solo conoce esta interfaz; la implementación (HTTP) está en la capa Api.
/// Así el controller no tiene lógica de cookies y el token nunca viaja en el JSON.
/// </summary>
public interface ICookieSesionService
{
    /// <summary>HU-01 criterio 6: guarda el token en la cookie al iniciar sesión.</summary>
    void Guardar(string token);

    /// <summary>HU-02: lee el token que trae la cookie de la petición (null si no hay cookie).</summary>
    string? LeerToken();

    /// <summary>HU-02: borra la cookie del navegador al cerrar sesión.</summary>
    void Eliminar();
}
