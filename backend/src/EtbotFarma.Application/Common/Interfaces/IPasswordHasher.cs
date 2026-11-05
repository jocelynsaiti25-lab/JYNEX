namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// Encripta y verifica contraseñas. La implementación está en Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string GenerarHash(string contrasenia);

    bool Verificar(string contraseniaHash, string contrasenia);
}
