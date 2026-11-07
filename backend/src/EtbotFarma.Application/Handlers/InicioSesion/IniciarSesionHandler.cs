using EtbotFarma.Application.Commands.InicioSesion;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.Dtos.InicioSesion;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Handlers.InicioSesion;

/// <summary>
/// HU-01: inicia la sesión de un usuario.
/// El handler solo COORDINA los pasos: no habla con la base de datos directamente
/// (usa interfaces) y las reglas de negocio están en las entidades del Domain.
/// Los datos YA llegan validados: MediatR ejecutó IniciarSesionValidator antes
/// (ver Common/Behaviors/ValidationBehavior.cs).
/// </summary>
public sealed class IniciarSesionHandler(
    IUsuarioRepository usuarios,
    ISesionRepository sesiones,
    IPasswordHasher passwordHasher,
    ITokenSesionService tokenService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj,
    ICookieSesionService cookieSesion,
    ConfiguracionSesion configuracion) : ICommandHandler<IniciarSesionCommand, UsuarioSesionDto>
{
    public async Task<UsuarioSesionDto> Handle(IniciarSesionCommand command, CancellationToken cancellationToken)
    {
        // 1. Buscar al usuario
        var usuario = await usuarios.ObtenerPorNombreUsuarioAsync(command.NombreUsuario.Trim(), cancellationToken);

        // 2. Verificar que exista, que esté activo y que la contraseña sea correcta.
        //    Si algo falla, el mensaje es el mismo (no se revela qué dato falló).
        if (usuario is null
            || !usuario.PuedeIniciarSesion()
            || !passwordHasher.Verificar(usuario.ContraseniaHash, command.Contrasenia))
        {
            throw new CredencialesInvalidasException();
        }

        // 3. Crear la sesión (se guarda el HASH del token, no el token)
        var token = tokenService.GenerarToken();
        var sesion = Sesion.Iniciar(usuario, tokenService.ObtenerHash(token), reloj.UtcNow.UtcDateTime, configuracion.InactividadMaxima);

        await sesiones.AgregarAsync(sesion, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        // 4. Guardar el token en la cookie segura (el token NO viaja en el JSON)
        cookieSesion.Guardar(token);

        // 5. Devolver los datos del usuario
        return new UsuarioSesionDto(
            usuario.Id,
            usuario.NombreUsuario,
            usuario.NombreCompleto,
            usuario.Rol.Nombre);
    }
}
