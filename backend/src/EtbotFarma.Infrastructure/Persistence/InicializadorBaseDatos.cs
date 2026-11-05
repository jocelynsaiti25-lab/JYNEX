using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EtbotFarma.Infrastructure.Persistence;

/// <summary>
/// Crea el usuario administrador inicial la primera vez que arranca la API,
/// solo si todavía no existe ningún usuario. Sin él nadie podría iniciar sesión,
/// porque la creación de usuarios (HU-03) requiere estar dentro del sistema.
///
/// La contraseña se lee de appsettings.Local.json (archivo que NO se sube a GitHub).
/// </summary>
public static class InicializadorBaseDatos
{
    public static async Task InicializarBaseDatosAsync(this IServiceProvider servicios)
    {
        using var scope = servicios.CreateScope();
        var proveedor = scope.ServiceProvider;
        var logger = proveedor.GetRequiredService<ILoggerFactory>().CreateLogger("InicializadorBaseDatos");
        var configuracion = proveedor.GetRequiredService<IConfiguration>();

        if (string.IsNullOrWhiteSpace(configuracion.GetConnectionString("EtbotFarma")))
        {
            logger.LogWarning("No hay cadena de conexión 'EtbotFarma'. Configúrala en appsettings.Local.json.");
            return;
        }

        var contexto = proveedor.GetRequiredService<EtbotFarmaDbContext>();

        if (await contexto.Usuarios.AnyAsync())
        {
            return; // Ya hay usuarios: no se hace nada
        }

        var admin = configuracion.GetSection("AdministradorInicial");
        var contrasenia = admin["Contrasenia"];

        if (string.IsNullOrWhiteSpace(contrasenia))
        {
            logger.LogWarning("No hay usuarios y falta AdministradorInicial:Contrasenia en appsettings.Local.json.");
            return;
        }

        var rolAdministrador = await contexto.Roles.FirstAsync(r => r.Id == Rol.Administrador);
        var passwordHasher = proveedor.GetRequiredService<IPasswordHasher>();

        var usuario = Usuario.Crear(
            nombre: admin["Nombre"] ?? "Administrador",
            apellidos: admin["Apellidos"] ?? "del Sistema",
            nombreUsuario: admin["NombreUsuario"] ?? "admin",
            contraseniaHash: passwordHasher.GenerarHash(contrasenia),
            telefono: admin["Telefono"] ?? "0000000000",
            rol: rolAdministrador,
            fechaRegistro: DateTime.UtcNow);

        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();

        logger.LogInformation("Usuario administrador inicial '{NombreUsuario}' creado.", usuario.NombreUsuario);
    }
}
