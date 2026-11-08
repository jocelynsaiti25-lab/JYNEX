using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Application.Tests.Falsos;

/// <summary>"Hash" falso: antepone "hash:" a la contraseña. Cuenta cuántas veces se verificó.</summary>
public sealed class PasswordHasherFalso : IPasswordHasher
{
    public int VecesVerificado { get; private set; }

    public string GenerarHash(string contrasenia) => "hash:" + contrasenia;

    public bool Verificar(string contraseniaHash, string contrasenia)
    {
        VecesVerificado++;
        return contraseniaHash == "hash:" + contrasenia;
    }
}
