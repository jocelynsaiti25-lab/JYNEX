using EtbotFarma.Application.CierreSesion.Commands;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.CierreSesion.Handlers;

/// <summary>
/// HU-02: cierra la sesión del lado del servidor.
/// El handler solo COORDINA: la regla de "cerrar" está en la entidad Sesion (Domain)
/// y el acceso a MySQL está detrás de ISesionRepository (Infrastructure).
/// </summary>
public sealed class CerrarSesionHandler(
    ICookieSesionService cookieSesion,
    ISesionRepository sesiones,
    ITokenSesionService tokenService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<CerrarSesionCommand, bool>
{
    public async Task<bool> Handle(CerrarSesionCommand command, CancellationToken cancellationToken)
    {
        var sesionCerrada = false;

        // 1. Leer el token de la cookie (puede no venir: escenario 10, la pantalla quedó abierta)
        var token = cookieSesion.LeerToken();

        if (!string.IsNullOrWhiteSpace(token))
        {
            // 2. Buscar la sesión por el HASH del token (el token original nunca se guarda en MySQL)
            var sesion = await sesiones.ObtenerPorTokenHashAsync(tokenService.ObtenerHash(token), cancellationToken);

            // 3. Escenario 1: invalidar la sesión EN EL SERVIDOR guardando la fecha de cierre.
            //    Desde ese momento la cookie anterior ya no sirve (escenarios 6 y 9).
            //    Si la sesión ya estaba cerrada o expirada (escenario 10) no se hace nada y no hay error.
            if (sesion is not null && !sesion.Cerrada)
            {
                sesion.Cerrar(reloj.UtcNow.UtcDateTime);
                await sesiones.ActualizarAsync(sesion, cancellationToken);
                await unitOfWork.GuardarCambiosAsync(cancellationToken);
                sesionCerrada = true;
            }
        }

        // 4. Siempre se borra la cookie del navegador, haya o no sesión (escenarios 10 y 13)
        cookieSesion.Eliminar();

        return sesionCerrada;
    }
}
