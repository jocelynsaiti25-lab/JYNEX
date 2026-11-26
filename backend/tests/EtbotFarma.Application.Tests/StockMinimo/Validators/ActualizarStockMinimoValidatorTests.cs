using EtbotFarma.Application.StockMinimo.Commands;
using EtbotFarma.Application.StockMinimo.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.StockMinimo.Validators;

/// <summary>Pruebas del validator de HU-11 (escenario 1).</summary>
public class ActualizarStockMinimoValidatorTests
{
    private readonly ActualizarStockMinimoValidator _validator = new();

    [Fact]
    public void Con_datos_correctos_no_hay_errores()
    {
        _validator.TestValidate(new ActualizarStockMinimoCommand(1, 10)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Cero_es_un_valor_valido()
    {
        _validator.TestValidate(new ActualizarStockMinimoCommand(1, 0)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Sin_stock_minimo_marca_obligatorio()
    {
        var resultado = _validator.TestValidate(new ActualizarStockMinimoCommand(1, null));

        resultado.ShouldHaveValidationErrorFor(c => c.NuevoStockMinimo)
            .WithErrorMessage("El stock mínimo es obligatorio.");
    }

    [Fact]
    public void Stock_minimo_negativo_marca_error()
    {
        var resultado = _validator.TestValidate(new ActualizarStockMinimoCommand(1, -1));

        resultado.ShouldHaveValidationErrorFor(c => c.NuevoStockMinimo)
            .WithErrorMessage("El stock mínimo debe ser un número entero de 0 o más.");
    }

    [Fact]
    public void Id_medicamento_invalido_marca_error()
    {
        var resultado = _validator.TestValidate(new ActualizarStockMinimoCommand(0, 10));

        resultado.ShouldHaveValidationErrorFor(c => c.IdMedicamento).WithErrorMessage("El medicamento no existe.");
    }
}
