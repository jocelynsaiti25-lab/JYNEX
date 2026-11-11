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

    /// <summary>Texto que recibió ListarAsync (HU-04), para revisar que el handler lo limpió.</summary>
    public string? UltimaBusqueda { get; private set; }

    /// <summary>HU-04: igual que MySQL: coincidencia parcial, sin importar mayúsculas, ordenado por nombre.</summary>
    public Task<IReadOnlyList<Usuario>> ListarAsync(string? nombreUsuarioContiene, CancellationToken cancellationToken)
    {
        UltimaBusqueda = nombreUsuarioContiene;
        IReadOnlyList<Usuario> resultado = Usuarios
            .Where(u => nombreUsuarioContiene is null
                || u.NombreUsuario.Contains(nombreUsuarioContiene, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.Nombre).ThenBy(u => u.Apellidos).ThenBy(u => u.NombreUsuario)
            .ToList();
        return Task.FromResult(resultado);
    }
}
