using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.Dtos.InicioSesion;
using EtbotFarma.Application.Queries.InicioSesion;

namespace EtbotFarma.Application.Handlers.InicioSesion;

/// <summary>
/// HU-01: devuelve el usuario de la sesión actual. Solo LEE (query): no modifica nada.
/// </summary>
public sealed class ObtenerSesionActualHandler(IUsuarioActual usuarioActual)
    : IQueryHandler<ObtenerSesionActualQuery, UsuarioSesionDto>
{
    public Task<UsuarioSesionDto> Handle(ObtenerSesionActualQuery query, CancellationToken cancellationToken)
    {
        // 1. Sin sesión válida no hay usuario actual
        if (usuarioActual.IdUsuario is not int idUsuario)
        {
            throw new NoAutenticadoException();
        }

        // 2. Devolver sus datos
        var usuario = new UsuarioSesionDto(
            idUsuario,
            usuarioActual.NombreUsuario,
            usuarioActual.NombreCompleto,
            usuarioActual.Rol);

        return Task.FromResult(usuario);
    }
}
