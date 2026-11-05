using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EtbotFarma.Infrastructure.Persistence.Repositorios;

/// <summary>
/// Consultas de sesiones en MySQL. Solo obtiene y devuelve datos; no tiene reglas de negocio.
/// </summary>
public sealed class SesionRepository(EtbotFarmaDbContext contexto) : ISesionRepository
{
    public async Task AgregarAsync(Sesion sesion, CancellationToken cancellationToken)
        => await contexto.Sesiones.AddAsync(sesion, cancellationToken);

    public Task<Sesion?> ObtenerPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => contexto.Sesiones
            .Include(s => s.Usuario)
                .ThenInclude(u => u.Rol)
            .FirstOrDefaultAsync(s => s.Token == tokenHash, cancellationToken);
}
