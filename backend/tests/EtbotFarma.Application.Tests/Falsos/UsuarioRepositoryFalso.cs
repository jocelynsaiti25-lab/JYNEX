using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class UsuarioRepositoryFalso : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = [];

    /// <summary>Usuarios que se guardaron con AgregarAsync (HU-03).</summary>
    public List<Usuario> Agregados { get; } = [];

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.FirstOrDefault(u =>
            string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.Any(u =>
            string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.Any(u =>
            string.Equals(u.Correo, correo, StringComparison.OrdinalIgnoreCase)));

    public Task<int> AgregarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        Usuarios.Add(usuario);
        Agregados.Add(usuario);
        return Task.FromResult(100 + Agregados.Count);   // id "inventado", como lo daría MySQL
    }
}
