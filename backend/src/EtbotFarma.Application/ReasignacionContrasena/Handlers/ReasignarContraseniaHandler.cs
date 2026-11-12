using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.ReasignacionContrasena.Commands;
using EtbotFarma.Application.ReasignacionContrasena.Dtos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.ReasignacionContrasena.Handlers;

/// <summary>
/// HU-05: el Administrador establece una contraseña nueva a una cuenta.
/// El handler solo COORDINA: la regla "solo cuentas activas" está en Usuario (Domain)
/// y el acceso a MySQL está detrás de los repositorios (Infrastructure).
/// Los datos YA llegan validados: MediatR ejecutó ReasignarContraseniaValidator antes.
/// Solo el Administrador llega aquí: el controller tiene [Authorize(Roles = "Administrador")] (escenario 6).
///
/// Regla de negocio: contraseña + cierre de sesiones + auditoría se guardan en UNA sola transacción
/// (un solo COMMIT). Si algo falla antes, no queda ningún cambio a medias (escenario 15).
/// </summary>
public sealed class ReasignarContraseniaHandler(
    IUsuarioRepository usuarios,
    ISesionRepository sesiones,
    IAuditoriaRepository auditorias,
    IUsuarioActual usuarioActual,
    IPasswordHasher passwordHasher,
    ICookieSesionService cookieSesion,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<ReasignarContraseniaCommand, ContraseniaReasignadaDto>
{
    public async Task<ContraseniaReasignadaDto> Handle(ReasignarContraseniaCommand command, CancellationToken cancellationToken)
    {
        // 0. Quién hace el cambio: el administrador con sesión
        var idAdministrador = usuarioActual.IdUsuario ?? throw new NoAutenticadoException();
        var ahora = reloj.UtcNow.UtcDateTime;

        // 1. Escenario 7: la cuenta debe existir (404 "La cuenta no existe")
        var usuario = await usuarios.ObtenerPorIdAsync(command.IdUsuario, cancellationToken)
            ?? throw new NoEncontradoException("La cuenta no existe");

        // 2. Regla del Domain (escenario 8): solo cuentas activas → si no, 422 "La cuenta está desactivada".
        //    La contraseña se guarda ENCRIPTADA (hash), nunca en texto plano (escenario 1).
        usuario.ReasignarContrasenia(passwordHasher.GenerarHash(command.NuevaContrasenia));
        await usuarios.ActualizarContraseniaAsync(usuario, cancellationToken);

        // 3. Escenarios 1, 12 y 17: se cierran TODAS las sesiones abiertas del usuario
        //    (si era el propio Administrador, también la que está usando ahora).
        var sesionesCerradas = await sesiones.CerrarSesionesDelUsuarioAsync(usuario.Id, ahora, cancellationToken);

        // 4. Escenario 14: auditoría con quién, a qué cuenta y cuándo. SIN contraseña ni hash.
        var auditoria = Auditoria.Crear(
            idAdministrador,
            Auditoria.ReasignarContrasenia,
            Auditoria.TablaUsuario,
            usuario.Id,
            valorAnterior: null,
            valorNuevo: usuario.DatosParaAuditoria(),
            ahora);
        await auditorias.AgregarAsync(auditoria, cancellationToken);

        // 5. COMMIT una sola vez: contraseña + sesiones + auditoría juntos
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        // 6. Escenario 17: si cambió su propia contraseña, también se borra su cookie (debe volver a entrar)
        var eraSuPropiaCuenta = usuario.Id == idAdministrador;
        if (eraSuPropiaCuenta)
        {
            cookieSesion.Eliminar();
        }

        return new ContraseniaReasignadaDto(
            usuario.Id, usuario.NombreUsuario, usuario.NombreCompleto, sesionesCerradas, eraSuPropiaCuenta);
    }
}
