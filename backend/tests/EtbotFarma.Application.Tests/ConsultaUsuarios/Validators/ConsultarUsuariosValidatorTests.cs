using EtbotFarma.Application.ConsultaUsuarios.Queries;
using EtbotFarma.Application.ConsultaUsuarios.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.ConsultaUsuarios.Validators;

/// <summary>Pruebas del validator de HU-04: la búsqueda es opcional y de máximo 50 caracteres.</summary>
public class ConsultarUsuariosValidatorTests
{
    private readonly ConsultarUsuariosValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("lopez")]
    public void Sin_busqueda_o_con_busqueda_corta_no_hay_errores(string? busqueda)
    {
        _validator.TestValidate(new ConsultarUsuariosQuery(busqueda)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Una_busqueda_de_mas_de_50_caracteres_marca_error()
    {
        var resultado = _validator.TestValidate(new ConsultarUsuariosQuery(new string('a', 51)));

        resultado.ShouldHaveValidationErrorFor(q => q.Busqueda)
            .WithErrorMessage("La búsqueda no puede tener más de 50 caracteres.");
    }
}
