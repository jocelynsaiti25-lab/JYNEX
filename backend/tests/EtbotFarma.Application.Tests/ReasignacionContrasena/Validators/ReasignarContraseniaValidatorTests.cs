using EtbotFarma.Application.ReasignacionContrasena.Commands;
using EtbotFarma.Application.ReasignacionContrasena.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.ReasignacionContrasena.Validators;

/// <summary>Pruebas del validator de HU-05 (escenarios 9, 10 y 11).</summary>
public class ReasignarContraseniaValidatorTests
{
    private readonly ReasignarContraseniaValidator _validator = new();

    private static ReasignarContraseniaCommand Con(string nueva, string confirmar) => new(7, nueva, confirmar);

    [Fact]
    public void Con_una_contrasenia_valida_y_confirmada_no_hay_errores()
    {
        _validator.TestValidate(Con("Farmacia2027", "Farmacia2027")).ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>Escenario 11: los dos campos son obligatorios.</summary>
    [Fact]
    public void Con_campos_vacios_marca_cada_uno()
    {
        var resultado = _validator.TestValidate(Con("", ""));

        resultado.ShouldHaveValidationErrorFor(c => c.NuevaContrasenia).WithErrorMessage("La nueva contraseña es obligatoria.");
        resultado.ShouldHaveValidationErrorFor(c => c.ConfirmarContrasenia).WithErrorMessage("La confirmación de la contraseña es obligatoria.");
    }

    /// <summary>Escenario 9: misma política que HU-03, diciendo qué requisito falta.</summary>
    [Theory]
    [InlineData("Corta1", "La contraseña debe tener al menos 8 caracteres.")]
    [InlineData("sinmayuscula1", "La contraseña debe tener al menos una letra mayúscula.")]
    [InlineData("SINMINUSCULA1", "La contraseña debe tener al menos una letra minúscula.")]
    [InlineData("SinNumeroAqui", "La contraseña debe tener al menos un número.")]
    public void Una_contrasenia_que_no_cumple_la_politica_dice_que_falta(string nueva, string mensaje)
    {
        _validator.TestValidate(Con(nueva, nueva))
            .ShouldHaveValidationErrorFor(c => c.NuevaContrasenia).WithErrorMessage(mensaje);
    }

    [Fact]
    public void Una_contrasenia_de_mas_de_100_caracteres_marca_error()
    {
        var larga = "Aa1" + new string('x', 98);

        _validator.TestValidate(Con(larga, larga))
            .ShouldHaveValidationErrorFor(c => c.NuevaContrasenia).WithErrorMessage("La contraseña no puede tener más de 100 caracteres.");
    }

    /// <summary>Escenario 10: las contraseñas deben coincidir.</summary>
    [Fact]
    public void Si_no_coinciden_marca_Las_contrasenias_no_coinciden()
    {
        _validator.TestValidate(Con("Farmacia2027", "Farmacia2028"))
            .ShouldHaveValidationErrorFor(c => c.ConfirmarContrasenia).WithErrorMessage("Las contraseñas no coinciden");
    }
}
