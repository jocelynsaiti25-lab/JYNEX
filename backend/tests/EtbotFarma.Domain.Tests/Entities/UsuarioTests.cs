using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

public class UsuarioTests
{
    private static readonly Rol Empleado = Rol.Reconstruir(Rol.Empleado, "Empleado");

    [Fact]
    public void Usuario_nuevo_esta_activo_y_puede_iniciar_sesion()
    {
        var usuario = Usuario.Crear("Ana", "López", "ana", "ana@etbotfarma.com", "hash", "5512345678", Empleado, DateTime.UtcNow);

        Assert.True(usuario.Activo);
        Assert.True(usuario.PuedeIniciarSesion());
        Assert.Equal("Ana López", usuario.NombreCompleto);
    }

    [Fact]
    public void Usuario_desactivado_no_puede_iniciar_sesion()
    {
        var usuario = Usuario.Crear("Ana", "López", "ana", "ana@etbotfarma.com", "hash", "5512345678", Empleado, DateTime.UtcNow);

        usuario.Desactivar();

        Assert.False(usuario.PuedeIniciarSesion());
    }

    [Fact]
    public void No_se_puede_crear_sin_nombre_de_usuario()
    {
        Assert.Throws<DomainException>(
            () => Usuario.Crear("Ana", "López", " ", "ana@etbotfarma.com", "hash", "5512345678", Empleado, DateTime.UtcNow));
    }

    [Fact]
    public void No_se_puede_crear_sin_correo()
    {
        Assert.Throws<DomainException>(
            () => Usuario.Crear("Ana", "López", "ana", "  ", "hash", "5512345678", Empleado, DateTime.UtcNow));
    }

    /// <summary>HU-03: el correo se guarda en minúsculas y sin espacios para poder compararlo.</summary>
    [Fact]
    public void El_correo_se_guarda_en_minusculas_y_sin_espacios()
    {
        var usuario = Usuario.Crear("Ana", "López", "ana", "  Ana.Lopez@GMAIL.com ", "hash", "5512345678", Empleado, DateTime.UtcNow);

        Assert.Equal("ana.lopez@gmail.com", usuario.Correo);
    }

    /// <summary>HU-03: la auditoría identifica la cuenta pero NUNCA lleva la contraseña, su hash ni el teléfono.</summary>
    [Fact]
    public void Datos_para_auditoria_no_incluyen_contrasenia_ni_telefono()
    {
        var usuario = Usuario.Crear("Ana", "López", "ana", "ana@etbotfarma.com", "hash-secreto", "5512345678", Empleado, DateTime.UtcNow);

        var datos = usuario.DatosParaAuditoria();

        Assert.Contains("\"nombreUsuario\":\"ana\"", datos);
        Assert.Contains("\"correo\":\"ana@etbotfarma.com\"", datos);
        Assert.Contains("López", datos);   // acentos legibles
        Assert.DoesNotContain("hash-secreto", datos);
        Assert.DoesNotContain("5512345678", datos);
    }
}
