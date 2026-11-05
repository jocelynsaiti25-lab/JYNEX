using System.Security.Cryptography;
using System.Text;
using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Infrastructure.Seguridad;

/// <summary>
/// Token de sesión:
///   - GenerarToken: 32 bytes aleatorios (imposible de adivinar). Este valor va en la cookie.
///   - ObtenerHash: SHA-256 del token. Esto es lo que se guarda en la base de datos.
/// </summary>
public sealed class TokenSesionService : ITokenSesionService
{
    public string GenerarToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public string ObtenerHash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
