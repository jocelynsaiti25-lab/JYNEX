using EtbotFarma.Application.InicioSesion.Commands;
using EtbotFarma.Application.InicioSesion.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.InicioSesion.Validators;

/// <summary>Pruebas del validator de HU-01 (criterio 1: datos obligatorios).</summary>
public class IniciarSesionValidatorTests
{
    private readonly IniciarSesionValidator _validator = new();

    [Fact]
    public void Con_usuario_y_contrasenia_no_hay_errores()
    {
        var resultado = _validator.TestValidate(new IniciarSesionCommand("admin", "Secreta123"));

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Sin_nombre_de_usuario_marca_el_error()
    {
        var resultado = _validator.TestValidate(new IniciarSesionCommand("", "Secreta123"));

        resultado.ShouldHaveValidationErrorFor(command => command.NombreUsuario)
            .WithErrorMessage("El nombre de usuario es obligatorio.");
        resultado.ShouldNotHaveValidationErrorFor(command => command.Contrasenia);
    }

    [Fact]
    public void Contrasenia_con_solo_espacios_marca_el_error()
    {
        var resultado = _validator.TestValidate(new IniciarSesionCommand("admin", "   "));

        resultado.ShouldHaveValidationErrorFor(command => command.Contrasenia)
            .WithErrorMessage("La contraseña es obligatoria.");
    }
}
