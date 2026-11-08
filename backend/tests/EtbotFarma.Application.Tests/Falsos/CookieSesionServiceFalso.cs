using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Application.Tests.Falsos;

/// <summary>Guarda en memoria el token que el handler "escribió" en la cookie.</summary>
public sealed class CookieSesionServiceFalso : ICookieSesionService
{
    public string? TokenGuardado { get; private set; }

    public void Guardar(string token) => TokenGuardado = token;
}
