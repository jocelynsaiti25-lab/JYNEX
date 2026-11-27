using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using EtbotFarma.Application.SalidaMedicamentos.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.SalidaMedicamentos.Validators;

public class SalidaMedicamentoValidatorTests
{
    private readonly RegistrarSalidaMedicamentoValidator _registrar = new();
    private readonly ProponerSalidaMedicamentoValidator _proponer = new();

    [Theory]
    [InlineData("1")]
    [InlineData("25")]
    [InlineData(" 7 ")]
    public void Cantidad_entera_positiva_es_valida(string cantidad)
    {
        _registrar.TestValidate(new RegistrarSalidaMedicamentoCommand(1, cantidad)).ShouldNotHaveAnyValidationErrors();
        _proponer.TestValidate(new ProponerSalidaMedicamentoQuery(1, cantidad)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cantidad_vacia_marca_campo_obligatorio(string cantidad)
    {
        _registrar.TestValidate(new RegistrarSalidaMedicamentoCommand(1, cantidad))
            .ShouldHaveValidationErrorFor(c => c.Cantidad)
            .WithErrorMessage("Este campo es obligatorio");
        _proponer.TestValidate(new ProponerSalidaMedicamentoQuery(1, cantidad))
            .ShouldHaveValidationErrorFor(c => c.Cantidad)
            .WithErrorMessage("Este campo es obligatorio");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("2.5")]
    [InlineData("abc")]
    [InlineData("1e3")]
    [InlineData("+4")]
    public void Cantidad_no_entera_o_no_positiva_marca_error(string cantidad)
    {
        _registrar.TestValidate(new RegistrarSalidaMedicamentoCommand(1, cantidad))
            .ShouldHaveValidationErrorFor(c => c.Cantidad)
            .WithErrorMessage("La cantidad debe ser un número entero mayor que 0");
        _proponer.TestValidate(new ProponerSalidaMedicamentoQuery(1, cantidad))
            .ShouldHaveValidationErrorFor(c => c.Cantidad)
            .WithErrorMessage("La cantidad debe ser un número entero mayor que 0");
    }

    [Fact]
    public void Sin_medicamento_marca_campo_obligatorio()
    {
        _registrar.TestValidate(new RegistrarSalidaMedicamentoCommand(0, "5"))
            .ShouldHaveValidationErrorFor(c => c.IdMedicamento)
            .WithErrorMessage("Este campo es obligatorio");
    }
}
