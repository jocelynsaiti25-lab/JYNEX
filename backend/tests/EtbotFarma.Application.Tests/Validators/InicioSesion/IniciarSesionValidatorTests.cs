using EtbotFarma.Application.Commands.InicioSesion;
using EtbotFarma.Application.Validators.InicioSesion;

namespace EtbotFarma.Application.Tests.Validators.InicioSesion;

/// <summary>Pruebas del validador de HU-01 (criterio 1: datos obligatorios).</summary>
public class IniciarSesionValidatorTests
{
    private readonly IniciarSesionValidator _validator = new();

    [Fact]
    public void Con_usuario_y_contrasenia_no_hay_errores()
    {
        var errores = _validator.Validar(new IniciarSesionCommand("admin", "Secreta123"));

        Assert.Empty(errores);
    }

    [Fact]
    public void Sin_datos_devuelve_los_dos_errores()
    {
        var errores = _validator.Validar(new IniciarSesionCommand("", " "));

        Assert.Contains("El nombre de usuario es obligatorio.", errores);
        Assert.Contains("La contraseña es obligatoria.", errores);
    }
}
