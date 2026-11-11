using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.CreacionUsuarios.Commands;
using EtbotFarma.Application.CreacionUsuarios.Dtos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.CreacionUsuarios.Handlers;

/// <summary>
/// HU-03: crea la cuenta de una persona del personal con su rol.
/// El handler solo COORDINA los pasos: no habla con la base de datos directamente (usa interfaces).
/// Los datos YA llegan validados: MediatR ejecutó CrearUsuarioValidator antes.
/// Solo el Administrador llega aquí: el controller tiene [Authorize(Roles = "Administrador")] (criterio 1).
/// </summary>
public sealed class CrearUsuarioHandler(
    IUsuarioRepository usuarios,
    IRolRepository roles,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<CrearUsuarioCommand, UsuarioCreadoDto>
{
    public async Task<UsuarioCreadoDto> Handle(CrearUsuarioCommand command, CancellationToken cancellationToken)
    {
        var nombreUsuario = command.NombreUsuario.Trim();
        var correo = Usuario.NormalizarCorreo(command.Correo);

        // 1. El nombre de usuario no se repite: con él se inicia sesión (HU-03 escenario 10)
        if (await usuarios.ExisteNombreUsuarioAsync(nombreUsuario, cancellationToken))
        {
            throw new DomainException("El nombre de usuario ya está en uso, elige otro.");
        }

        // 2. El correo identifica a la persona: si ya existe, la persona ya está registrada (escenario 7).
        //    El nombre y el teléfono NO se revisan: dos personas pueden llamarse igual (escenario 9).
        if (await usuarios.ExisteCorreoAsync(correo, cancellationToken))
        {
            throw new DomainException("Este correo ya está registrado.");
        }

        // 3. Obtener el rol (Administrador o Empleado)
        var rol = await roles.ObtenerPorIdAsync(command.IdRol, cancellationToken)
            ?? throw new DomainException("El rol seleccionado no existe.");

        // 4. Crear el usuario activo con la contraseña ENCRIPTADA (nunca en texto plano, criterio 5)
        var usuario = Usuario.Crear(
            command.Nombre,
            command.Apellidos,
            nombreUsuario,
            correo,
            passwordHasher.GenerarHash(command.Contrasenia),
            command.Telefono,
            rol,
            reloj.UtcNow.UtcDateTime);

        // 5. Guardar (COMMIT una sola vez)
        var idUsuario = await usuarios.AgregarAsync(usuario, cancellationToken);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        // 6. Devolver los datos de la cuenta (sin contraseña)
        return new UsuarioCreadoDto(idUsuario, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Correo, rol.Nombre);
    }
}
