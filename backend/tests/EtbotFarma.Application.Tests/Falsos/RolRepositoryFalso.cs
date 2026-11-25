using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

/// <summary>Tiene los mismos dos roles que el script de MySQL.</summary>
public sealed class RolRepositoryFalso : IRolRepository
{
    private readonly List<Rol> _roles =
    [
        Rol.Reconstruir(Rol.Administrador, "Administrador"),
        Rol.Reconstruir(Rol.Empleado, "Empleado")
    ];

    public Task<Rol?> ObtenerPorIdAsync(int idRol, CancellationToken cancellationToken)
        => Task.FromResult(_roles.FirstOrDefault(r => r.Id == idRol));
}
