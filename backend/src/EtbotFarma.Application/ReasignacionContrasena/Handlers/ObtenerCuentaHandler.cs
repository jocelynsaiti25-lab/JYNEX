using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.ReasignacionContrasena.Dtos;
using EtbotFarma.Application.ReasignacionContrasena.Queries;

namespace EtbotFarma.Application.ReasignacionContrasena.Handlers;

/// <summary>
/// HU-05: devuelve los datos de la cuenta para el formulario de reasignación. Solo LEE.
/// Escenario 7: si el id no existe (por ejemplo, escrito a mano en la dirección) → 404 "La cuenta no existe".
/// </summary>
public sealed class ObtenerCuentaHandler(IUsuarioRepository usuarios) : IQueryHandler<ObtenerCuentaQuery, CuentaDto>
{
    public async Task<CuentaDto> Handle(ObtenerCuentaQuery query, CancellationToken cancellationToken)
    {
        // 1. Buscar la cuenta
        var usuario = await usuarios.ObtenerPorIdAsync(query.IdUsuario, cancellationToken)
            ?? throw new NoEncontradoException("La cuenta no existe");

        // 2. Devolver solo lo que se muestra (sin contraseña ni hash)
        return new CuentaDto(
            usuario.Id, usuario.NombreCompleto, usuario.NombreUsuario, usuario.Correo, usuario.Rol.Nombre, usuario.Activo);
    }
}
