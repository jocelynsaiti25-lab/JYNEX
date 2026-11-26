using EtbotFarma.Application.EdicionMedicamentos.Commands;
using EtbotFarma.Application.EdicionMedicamentos.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.EdicionMedicamentos.Validators;

/// <summary>Pruebas del validator de HU-09 (escenario 7).</summary>
public class EditarMedicamentoValidatorTests
{
    private readonly EditarMedicamentoValidator _validator = new();

    private static EditarMedicamentoCommand Con(
        string nombreComercial = "Tempra", string nombreSustancia = "Paracetamol",
        string presentacion = "Tabletas", string concentracion = "500 mg")
        => new(1, nombreComercial, nombreSustancia, presentacion, concentracion, EsControlado: false);

    [Fact]
    public void Con_datos_validos_no_hay_errores()
    {
        _validator.TestValidate(Con()).ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>Escenario 7: cada campo descriptivo obligatorio vacío muestra su propio mensaje.</summary>
    [Fact]
    public void Con_campos_vacios_marca_cada_uno()
    {
        var resultado = _validator.TestValidate(Con("", "", "", ""));

        resultado.ShouldHaveValidationErrorFor(c => c.NombreComercial).WithErrorMessage("Este campo es obligatorio.");
        resultado.ShouldHaveValidationErrorFor(c => c.NombreSustancia).WithErrorMessage("Este campo es obligatorio.");
        resultado.ShouldHaveValidationErrorFor(c => c.Presentacion).WithErrorMessage("Este campo es obligatorio.");
        resultado.ShouldHaveValidationErrorFor(c => c.Concentracion).WithErrorMessage("Este campo es obligatorio.");
    }

    [Fact]
    public void Con_id_de_medicamento_invalido_marca_error()
    {
        _validator.TestValidate(Con() with { IdMedicamento = 0 })
            .ShouldHaveValidationErrorFor(c => c.IdMedicamento);
    }
}
