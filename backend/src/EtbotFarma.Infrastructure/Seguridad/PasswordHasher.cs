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

    /// <summary>
    /// Hash de una contraseña aleatoria que nadie conoce. Se usa cuando el usuario no existe:
    /// así Verificar hace el mismo cálculo PBKDF2 (y tarda lo mismo) que con un usuario real.
    /// </summary>
    private readonly string _hashFicticio;

    public PasswordHasher()
    {
        _hashFicticio = _hasher.HashPassword(new object(), Guid.NewGuid().ToString());
    }

    public string GenerarHash(string contrasenia)
        => _hasher.HashPassword(new object(), contrasenia);

    public bool Verificar(string contraseniaHash, string contrasenia)
    {
        // Sin hash (usuario inexistente): se calcula igual contra el hash ficticio, pero nunca deja entrar
        if (string.IsNullOrWhiteSpace(contraseniaHash))
        {
            _hasher.VerifyHashedPassword(new object(), _hashFicticio, contrasenia);
            return false;
        }

        var resultado = _hasher.VerifyHashedPassword(new object(), contraseniaHash, contrasenia);
        return resultado != PasswordVerificationResult.Failed;
    }
}
