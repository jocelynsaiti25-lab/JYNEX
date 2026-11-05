using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.Features.Sesiones.HU01_ValidarSesion;

/// <summary>
/// HU-01: valida la sesión de la cookie. Solo coordina; las reglas
/// (vigencia por inactividad, cuenta activa) están en Sesion y Usuario.
/// </summary>
public sealed class ValidarSesionHandler(
    ISesionRepository sesiones,
    ITokenSesionService tokenService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj,
    ConfiguracionSesion configuracion) : ICommandHandler<ValidarSesionCommand, ValidarSesionDto?>
{
    public async Task<ValidarSesionDto?> Handle(ValidarSesionCommand command, CancellationToken cancellationToken)
    {
        // 1. Sin token no hay sesión
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return null;
        }

        // 2. Buscar la sesión por el hash del token
        var sesion = await sesiones.ObtenerPorTokenHashAsync(tokenService.ObtenerHash(command.Token), cancellationToken);
        if (sesion is null)
        {
            return null;
        }

        var ahora = reloj.UtcNow.UtcDateTime;

        // 3. Si expiró por inactividad o la cuenta ya no está activa, se cierra
        if (!sesion.EstaVigente(ahora, configuracion.InactividadMaxima) || !sesion.Usuario.PuedeIniciarSesion())
        {
            sesion.Cerrar(ahora);
            await unitOfWork.GuardarCambiosAsync(cancellationToken);
            return null;
        }

        // 4. Sesión válida: se reinicia el contador de inactividad
        sesion.RegistrarActividad(ahora);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new ValidarSesionDto(
            sesion.Usuario.Id,
            sesion.Usuario.NombreUsuario,
            sesion.Usuario.NombreCompleto,
            sesion.Usuario.Rol.Nombre);
    }
}
