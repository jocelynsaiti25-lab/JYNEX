namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// HU-01 criterio 6: guarda el token de la sesión en una cookie segura del navegador.
/// Application solo conoce esta interfaz; la implementación (HTTP) está en la capa Api.
/// Así el controller no tiene lógica de cookies y el token nunca viaja en el JSON.
/// </summary>
public interface ICookieSesionService
{
    void Guardar(string token);
}
