using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Application.Tests.Falsos;

/// <summary>Simula en memoria la cookie del navegador.</summary>
public sealed class CookieSesionServiceFalso : ICookieSesionService
{
    /// <summary>Token que el handler "escribió" en la cookie (HU-01).</summary>
    public string? TokenGuardado { get; private set; }

    /// <summary>Token que "trae" la petición. Las pruebas lo ponen a mano (HU-02).</summary>
    public string? TokenEnLaPeticion { get; set; }

    /// <summary>true si el handler pidió borrar la cookie (HU-02).</summary>
    public bool Eliminada { get; private set; }

    public void Guardar(string token) => TokenGuardado = token;

    public string? LeerToken() => TokenEnLaPeticion;

    public void Eliminar() => Eliminada = true;
}
