using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

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
        => Task.FromResult(Sesiones.FirstOrDefault(s => s.TokenHash == tokenHash));

    public int VecesActualizada { get; private set; }

    public Task ActualizarAsync(Sesion sesion, CancellationToken cancellationToken)
    {
        VecesActualizada++;
        return Task.CompletedTask;
    }
}

/// <summary>"Hash" falso: antepone "hash:" a la contraseña. Cuenta cuántas veces se verificó.</summary>
public sealed class PasswordHasherFalso : IPasswordHasher
{
    public int VecesVerificado { get; private set; }

    public string GenerarHash(string contrasenia) => "hash:" + contrasenia;

    public bool Verificar(string contraseniaHash, string contrasenia)
    {
        VecesVerificado++;
        return contraseniaHash == "hash:" + contrasenia;
    }
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

/// <summary>Guarda en memoria el token que el handler "escribió" en la cookie.</summary>
public sealed class CookieSesionServiceFalso : ICookieSesionService
{
    public string? TokenGuardado { get; private set; }

    public void Guardar(string token) => TokenGuardado = token;
}

/// <summary>Usuario actual configurable para las pruebas.</summary>
public sealed class UsuarioActualFalso : IUsuarioActual
{
    public int? IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}
