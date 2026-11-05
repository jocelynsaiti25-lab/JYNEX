using Microsoft.AspNetCore.Identity;
using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Infrastructure.Seguridad;

/// <summary>
/// Encripta contraseñas con el algoritmo de ASP.NET Core Identity
/// (PBKDF2 con sal aleatoria). La contraseña nunca se guarda en texto normal.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string GenerarHash(string contrasenia)
        => _hasher.HashPassword(new object(), contrasenia);

    public bool Verificar(string contraseniaHash, string contrasenia)
    {
        var resultado = _hasher.VerifyHashedPassword(new object(), contraseniaHash, contrasenia);
        return resultado != PasswordVerificationResult.Failed;
    }
}
