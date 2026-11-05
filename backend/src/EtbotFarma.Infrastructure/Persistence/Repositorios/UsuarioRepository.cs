using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas de usuarios en MySQL. Solo obtiene y devuelve datos; no tiene reglas de negocio.
/// </summary>
public sealed class UsuarioRepository(EtbotFarmaDbContext contexto) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken)
        => contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, cancellationToken);
}
