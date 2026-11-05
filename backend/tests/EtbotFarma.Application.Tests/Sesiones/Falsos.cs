using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Sesiones;

// =====================================================================
// Implementaciones FALSAS de las interfaces, solo para pruebas.
// Gracias a que los handlers dependen de interfaces (principio D de SOLID),
// se pueden probar sin base de datos.
// =====================================================================

public sealed class UsuarioRepositoryFalso : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = [];

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.FirstOrDefault(u =>
            string.Equals(u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)));
}

public sealed class SesionRepositoryFalso : ISesionRepository
{
    public List<Sesion> Sesiones { get; } = [];

    public Task AgregarAsync(Sesion sesion, CancellationToken cancellationToken)
    {
        Sesiones.Add(sesion);
        return Task.CompletedTask;
    }

    public Task<Sesion?> ObtenerPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => Task.FromResult(Sesiones.FirstOrDefault(s => s.Token == tokenHash));
}

/// <summary>"Hash" falso: antepone "hash:" a la contraseña.</summary>
public sealed class PasswordHasherFalso : IPasswordHasher
{
    public string GenerarHash(string contrasenia) => "hash:" + contrasenia;

    public bool Verificar(string contraseniaHash, string contrasenia) => contraseniaHash == "hash:" + contrasenia;
}

public sealed class TokenSesionServiceFalso : ITokenSesionService
{
    public string GenerarToken() => "token-123";

    public string ObtenerHash(string token) => "h:" + token;
}

public sealed class UnitOfWorkFalso : IUnitOfWork
{
    public int VecesGuardado { get; private set; }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        VecesGuardado++;
        return Task.CompletedTask;
    }
}

public sealed class RelojFalso(DateTimeOffset ahora) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = ahora;
}
