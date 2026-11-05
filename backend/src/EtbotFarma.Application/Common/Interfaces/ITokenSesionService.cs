namespace EtbotFarma.Application.Common.Interfaces;

/// <summary>
/// Genera el token aleatorio que va en la cookie y calcula su hash (lo que se guarda en la BD).
/// La implementación está en Infrastructure.
/// </summary>
public interface ITokenSesionService
{
    string GenerarToken();

    string ObtenerHash(string token);
}
