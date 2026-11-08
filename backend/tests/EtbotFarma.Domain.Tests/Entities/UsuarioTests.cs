using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

public class UsuarioTests
{
    private static readonly Rol Empleado = Rol.Reconstruir(Rol.Empleado, "Empleado");

    [Fact]
    public void Usuario_nuevo_esta_activo_y_puede_iniciar_sesion()
    {
        var usuario = Usuario.Crear("Ana", "López", "ana", "hash", "5512345678", Empleado, DateTime.UtcNow);

        Assert.True(usuario.Activo);
        Assert.True(usuario.PuedeIniciarSesion());
        Assert.Equal("Ana López", usuario.NombreCompleto);
    }

    [Fact]
    public void Usuario_desactivado_no_puede_iniciar_sesion()
    {
        var usuario = Usuario.Crear("Ana", "López", "ana", "hash", "5512345678", Empleado, DateTime.UtcNow);

        usuario.Desactivar();

        Assert.False(usuario.PuedeIniciarSesion());
    }

    [Fact]
    public void No_se_puede_crear_sin_nombre_de_usuario()
    {
        Assert.Throws<DomainException>(
            () => Usuario.Crear("Ana", "López", " ", "hash", "5512345678", Empleado, DateTime.UtcNow));
    }
}
