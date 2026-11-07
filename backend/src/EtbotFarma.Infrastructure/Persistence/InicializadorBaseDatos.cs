using EtbotFarma.Application.Common.Interfaces;
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
    private const int IdRolAdministrador = 1;

    public static async Task InicializarBaseDatosAsync(this IServiceProvider servicios)
    {
        await using var scope = servicios.CreateAsyncScope();
        var proveedor = scope.ServiceProvider;
        var logger = proveedor.GetRequiredService<ILoggerFactory>().CreateLogger("InicializadorBaseDatos");
        var configuracion = proveedor.GetRequiredService<IConfiguration>();

        if (string.IsNullOrWhiteSpace(configuracion.GetConnectionString("EtbotFarma")))
        {
            logger.LogWarning("No hay cadena de conexión 'EtbotFarma'. Configúrala en appsettings.Local.json.");
            return;
        }

        var conexion = proveedor.GetRequiredService<ConexionMySql>();

        // 1. ¿Ya hay usuarios? Entonces no se hace nada
        await using (var contar = await conexion.CrearComandoAsync("SELECT COUNT(*) FROM usuario;", CancellationToken.None))
        {
            if (Convert.ToInt64(await contar.ExecuteScalarAsync()) > 0)
            {
                return;
            }
        }

        var admin = configuracion.GetSection("AdministradorInicial");
        var contrasenia = admin["Contrasenia"];

        if (string.IsNullOrWhiteSpace(contrasenia))
        {
            logger.LogWarning("No hay usuarios y falta AdministradorInicial:Contrasenia en appsettings.Local.json.");
            return;
        }

        // 2. Crear el administrador con la contraseña encriptada
        var nombreUsuario = admin["NombreUsuario"] ?? "admin";
        var passwordHasher = proveedor.GetRequiredService<IPasswordHasher>();

        const string sql = """
            INSERT INTO usuario (nombre, apellidos, nombre_usuario, contrasenia_hash, telefono, id_rol, activo, fecha_registro)
            VALUES (@nombre, @apellidos, @nombreUsuario, @contraseniaHash, @telefono, @idRol, TRUE, @fechaRegistro);
            """;

        await using (var insertar = await conexion.CrearComandoAsync(sql, CancellationToken.None))
        {
            insertar.Parameters.AddWithValue("@nombre", admin["Nombre"] ?? "Administrador");
            insertar.Parameters.AddWithValue("@apellidos", admin["Apellidos"] ?? "del Sistema");
            insertar.Parameters.AddWithValue("@nombreUsuario", nombreUsuario);
            insertar.Parameters.AddWithValue("@contraseniaHash", passwordHasher.GenerarHash(contrasenia));
            insertar.Parameters.AddWithValue("@telefono", admin["Telefono"] ?? "0000000000");
            insertar.Parameters.AddWithValue("@idRol", IdRolAdministrador);
            insertar.Parameters.AddWithValue("@fechaRegistro", DateTime.UtcNow);
            await insertar.ExecuteNonQueryAsync();
        }

        // 3. COMMIT
        await conexion.GuardarCambiosAsync(CancellationToken.None);

        logger.LogInformation("Usuario administrador inicial '{NombreUsuario}' creado.", nombreUsuario);
    }
}
