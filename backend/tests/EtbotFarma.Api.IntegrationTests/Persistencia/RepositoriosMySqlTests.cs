using System.Security.Cryptography;
using System.Text;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Api.IntegrationTests.Persistencia;

/// <summary>
/// Pruebas DIRECTAS de los repositorios con SQL a mano y de las transacciones (ConexionMySql).
/// Usan MySQL real y la inyección de dependencias real (cada "scope" = una petición HTTP).
/// </summary>
[Trait("Category", "Integracion")]
public class RepositoriosMySqlTests(ApiConMySql api) : IClassFixture<ApiConMySql>
{
    private static readonly TimeSpan Veinte = TimeSpan.FromMinutes(20);

    /// <summary>Hash único de 64 caracteres para que cada prueba tenga su propia sesión.</summary>
    private static string HashUnico() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToLowerInvariant();

    private static DateTime AhoraSinMilisegundos()
    {
        var ahora = DateTime.UtcNow;
        return new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute, ahora.Second, DateTimeKind.Utc);
    }

    private async Task<Usuario> ObtenerAdminAsync()
    {
        await using var scope = api.Services.CreateAsyncScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        return (await usuarios.ObtenerPorNombreUsuarioAsync("admin", CancellationToken.None))!;
    }

    private async Task<Sesion?> BuscarSesionAsync(string tokenHash)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var sesiones = scope.ServiceProvider.GetRequiredService<ISesionRepository>();
        return await sesiones.ObtenerPorTokenHashAsync(tokenHash, CancellationToken.None);
    }

    [Fact]
    public async Task UsuarioRepository_devuelve_el_usuario_con_su_rol()
    {
        var admin = await ObtenerAdminAsync();

        Assert.NotNull(admin);
        Assert.Equal("admin", admin.NombreUsuario);
        Assert.Equal("Administrador", admin.Rol.Nombre);
        Assert.True(admin.Activo);
        Assert.NotEqual("Prueba123!", admin.ContraseniaHash);   // la contraseña está encriptada
    }

    [Fact]
    public async Task UsuarioRepository_con_usuario_inexistente_devuelve_null()
    {
        await using var scope = api.Services.CreateAsyncScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();

        Assert.Null(await usuarios.ObtenerPorNombreUsuarioAsync("no-existe", CancellationToken.None));
    }

    [Fact]
    public async Task UsuarioRepository_no_es_vulnerable_a_inyeccion_SQL()
    {
        await using var scope = api.Services.CreateAsyncScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();

        // Si el SQL se armara pegando texto, esto devolvería un usuario
        var resultado = await usuarios.ObtenerPorNombreUsuarioAsync("admin' OR '1'='1", CancellationToken.None);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task Con_COMMIT_la_sesion_queda_guardada_con_su_usuario_y_expiracion()
    {
        var admin = await ObtenerAdminAsync();
        var hash = HashUnico();
        var inicio = AhoraSinMilisegundos();

        // Petición 1: guardar y confirmar
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var sesiones = scope.ServiceProvider.GetRequiredService<ISesionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await sesiones.AgregarAsync(Sesion.Iniciar(admin, hash, inicio, Veinte), CancellationToken.None);
            await unitOfWork.GuardarCambiosAsync(CancellationToken.None);   // COMMIT
        }

        // Petición 2: leerla
        var sesion = await BuscarSesionAsync(hash);

        Assert.NotNull(sesion);
        Assert.Equal(hash, sesion.TokenHash);
        Assert.Equal("admin", sesion.Usuario.NombreUsuario);
        Assert.Equal("Administrador", sesion.Usuario.Rol.Nombre);
        Assert.Equal(inicio.AddMinutes(20), sesion.FechaExpiracion);
        Assert.False(sesion.Cerrada);
    }

    [Fact]
    public async Task Sin_COMMIT_no_se_guarda_nada_ROLLBACK()
    {
        var admin = await ObtenerAdminAsync();
        var hash = HashUnico();

        // Petición que escribe pero termina SIN GuardarCambiosAsync (como si hubiera un error)
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var sesiones = scope.ServiceProvider.GetRequiredService<ISesionRepository>();
            await sesiones.AgregarAsync(Sesion.Iniciar(admin, hash, AhoraSinMilisegundos(), Veinte), CancellationToken.None);
        }

        Assert.Null(await BuscarSesionAsync(hash));
    }

    [Fact]
    public async Task ActualizarAsync_guarda_el_cierre_de_la_sesion()
    {
        var admin = await ObtenerAdminAsync();
        var hash = HashUnico();
        var inicio = AhoraSinMilisegundos();

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var sesiones = scope.ServiceProvider.GetRequiredService<ISesionRepository>();
            await sesiones.AgregarAsync(Sesion.Iniciar(admin, hash, inicio, Veinte), CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GuardarCambiosAsync(CancellationToken.None);
        }

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var sesiones = scope.ServiceProvider.GetRequiredService<ISesionRepository>();
            var sesion = (await sesiones.ObtenerPorTokenHashAsync(hash, CancellationToken.None))!;
            sesion.Cerrar(inicio.AddMinutes(5));
            await sesiones.ActualizarAsync(sesion, CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GuardarCambiosAsync(CancellationToken.None);
        }

        var cerrada = await BuscarSesionAsync(hash);

        Assert.True(cerrada!.Cerrada);
        Assert.Equal(inicio.AddMinutes(5), cerrada.FechaCierre);
        Assert.False(cerrada.EstaVigente(inicio.AddMinutes(6)));
    }
}
