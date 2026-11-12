using EtbotFarma.Application.Common.Validacion;
using EtbotFarma.Application.CreacionUsuarios.Commands;
using EtbotFarma.Application.CreacionUsuarios.Validators;
using EtbotFarma.Application.ReasignacionContrasena.Commands;
using EtbotFarma.Application.ReasignacionContrasena.Validators;
using FluentValidation;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.Common;

/// <summary>
/// Pruebas de la política de contraseñas COMPARTIDA (ReglasContrasenia), la que usan HU-03 y HU-05:
///   de 8 a 15 caracteres, con mayúscula, minúscula, número y carácter especial.
/// Aquí se prueban TODOS los casos de la política una sola vez; los tests de cada validator
/// solo comprueban que la historia sí la está usando.
/// </summary>
public class ReglasContraseniaTests
{
    private sealed record Datos(string Contrasenia);

    /// <summary>Validator mínimo que solo aplica la política (igual que los validators reales).</summary>
    private sealed class Validador : AbstractValidator<Datos>
    {
        public Validador()
        {
            RuleLevelCascadeMode = CascadeMode.Stop;
            RuleFor(d => d.Contrasenia).ContraseniaSegura();
        }
    }

    private readonly Validador _validador = new();

    [Theory]
    [InlineData("Farma#26")]          // exactamente 8 caracteres (el mínimo)
    [InlineData("Farmacia#202601")]   // exactamente 15 caracteres (el máximo)
    [InlineData("Farma@26")]
    [InlineData("Farma$26")]
    [InlineData("Farma%26")]
    [InlineData("Farma!26")]
    [InlineData("Farma&26")]
    [InlineData("Etbot.Farma9")]      // otros signos (punto, guion...) también cuentan como especiales
    public void Acepta_contrasenias_que_cumplen_toda_la_politica(string contrasenia)
    {
        _validador.TestValidate(new Datos(contrasenia)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Fa#2026", "La contraseña debe tener al menos 8 caracteres.")]                // 7 caracteres
    [InlineData("Farmacia#2026XYZ", "La contraseña no puede tener más de 15 caracteres.")]    // 16 caracteres
    [InlineData("farmacia#2026", "La contraseña debe tener al menos una letra mayúscula.")]
    [InlineData("FARMACIA#2026", "La contraseña debe tener al menos una letra minúscula.")]
    [InlineData("Farmacia#Etbot", "La contraseña debe tener al menos un número.")]
    [InlineData("Farmacia2026", "La contraseña debe tener al menos un carácter especial (por ejemplo @, #, $, %, ! o &).")]
    [InlineData("Farmacia 2026", "La contraseña debe tener al menos un carácter especial (por ejemplo @, #, $, %, ! o &).")]  // el espacio no cuenta
    [InlineData("Farmaciañ2026", "La contraseña debe tener al menos un carácter especial (por ejemplo @, #, $, %, ! o &).")]  // la ñ es letra
    public void Rechaza_y_dice_que_requisito_falta(string contrasenia, string mensaje)
    {
        _validador.TestValidate(new Datos(contrasenia))
            .ShouldHaveValidationErrorFor(d => d.Contrasenia).WithErrorMessage(mensaje);
    }

    /// <summary>
    /// HU-03 (crear usuario) y HU-05 (reasignar contraseña) usan EXACTAMENTE la misma política:
    /// con la misma contraseña, los dos validators aceptan o rechazan igual y con el mismo mensaje.
    /// </summary>
    [Theory]
    [InlineData("Farma#26")]
    [InlineData("Farmacia#202601")]
    [InlineData("Fa#2026")]
    [InlineData("Farmacia#2026XYZ")]
    [InlineData("farmacia#2026")]
    [InlineData("FARMACIA#2026")]
    [InlineData("Farmacia#Etbot")]
    [InlineData("Farmacia2026")]
    public void HU03_y_HU05_aplican_la_misma_politica(string contrasenia)
    {
        var creacion = new CrearUsuarioValidator().TestValidate(new CrearUsuarioCommand(
            "María", "López", "mlopez", "maria@gmail.com", "5512345678", contrasenia, 2));
        var reasignacion = new ReasignarContraseniaValidator().TestValidate(
            new ReasignarContraseniaCommand(7, contrasenia, contrasenia));

        var mensajesCreacion = creacion.Errors
            .Where(e => e.PropertyName == nameof(CrearUsuarioCommand.Contrasenia)).Select(e => e.ErrorMessage);
        var mensajesReasignacion = reasignacion.Errors
            .Where(e => e.PropertyName == nameof(ReasignarContraseniaCommand.NuevaContrasenia)).Select(e => e.ErrorMessage);

        Assert.Equal(mensajesCreacion, mensajesReasignacion);
    }

    [Fact]
    public void Una_contrasenia_muy_larga_marca_el_maximo_de_15()
    {
        var larga = "Aa1#" + new string('x', 96);   // 100 caracteres: antes se aceptaba, ahora no

        _validador.TestValidate(new Datos(larga))
            .ShouldHaveValidationErrorFor(d => d.Contrasenia).WithErrorMessage("La contraseña no puede tener más de 15 caracteres.");
    }
}
