using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class TokenSesionServiceFalso : ITokenSesionService
{
    public string GenerarToken() => "token-123";

    public string ObtenerHash(string token) => "h:" + token;
}
