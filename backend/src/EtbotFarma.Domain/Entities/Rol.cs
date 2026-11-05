using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Entities;

/// <summary>
/// Tipo de usuario del sistema. Tabla: rol.
/// </summary>
public class Rol : Entity<int>
{
    public const int Administrador = 1;
    public const int Empleado = 2;

    public string Nombre { get; private set; } = string.Empty;

    // Constructor vacío que usa Entity Framework para leer de la base de datos
    private Rol() { }

    public Rol(int id, string nombre)
    {
        Id = id;
        Nombre = nombre;
    }
}
