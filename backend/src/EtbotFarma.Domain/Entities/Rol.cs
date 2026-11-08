using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Tipo de usuario del sistema. Tabla: rol.
/// Los roles se crean en la base de datos (etbotfarma_mysql_v2.sql), no desde el código:
/// por eso solo existe Reconstruir(...) y no Crear(...).
/// </summary>
public class Rol : Entity<int>
{
    /// <summary>Identificadores fijos de la tabla rol (INSERT del script SQL).</summary>
    public const int Administrador = 1;
    public const int Empleado = 2;

    public string Nombre { get; private set; } = string.Empty;

    private Rol() { }

    /// <summary>
    /// Arma un rol con los datos leídos de MySQL (lo usan los repositorios).
    /// No aplica reglas: los datos ya estaban guardados.
    /// </summary>
    public static Rol Reconstruir(int id, string nombre)
    {
        return new Rol
        {
            Id = id,
            Nombre = nombre
        };
    }
}
