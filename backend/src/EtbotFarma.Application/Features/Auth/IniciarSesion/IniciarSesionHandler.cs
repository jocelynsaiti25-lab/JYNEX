using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Features.Auth.IniciarSesion;

/// <summary>
/// HU-01: inicia la sesión de un usuario.
/// El handler solo COORDINA los pasos: no habla con la base de datos directamente
/// (usa interfaces) y las reglas de negocio están en las entidades del Domain.
/// </summary>
public sealed class IniciarSesionHandler(
    IUsuarioRepository usuarios,
    ISesionRepository sesiones,
    IPasswordHasher passwordHasher,
    ITokenSesionService tokenService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<IniciarSesionCommand, SesionIniciadaDto>
{
    private readonly IniciarSesionValidator _validator = new();

    public async Task<SesionIniciadaDto> Handle(IniciarSesionCommand command, CancellationToken cancellationToken)
    {
        // 1. Validar los datos recibidos
        var errores = _validator.Validar(command);
        if (errores.Count > 0)
        {
            throw new ValidacionException(errores);
        }

        // 2. Buscar al usuario
        var usuario = await usuarios.ObtenerPorNombreUsuarioAsync(command.NombreUsuario.Trim(), cancellationToken);

        // 3. Verificar que exista, que esté activo y que la contraseña sea correcta.
        //    Si algo falla, el mensaje es el mismo (no se revela qué dato falló).
        if (usuario is null
            || !usuario.PuedeIniciarSesion()
            || !passwordHasher.Verificar(usuario.ContraseniaHash, command.Contrasenia))
        {
            throw new CredencialesInvalidasException();
        }

        // 4. Crear la sesión (se guarda el HASH del token, no el token)
        var token = tokenService.GenerarToken();
        var sesion = Sesion.Iniciar(usuario, tokenService.ObtenerHash(token), reloj.UtcNow.UtcDateTime);

        await sesiones.AgregarAsync(sesion, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        // 5. Devolver el resultado
        return new SesionIniciadaDto(
            token,
            usuario.Id,
            usuario.NombreUsuario,
            usuario.NombreCompleto,
            usuario.Rol.Nombre);
    }
}
