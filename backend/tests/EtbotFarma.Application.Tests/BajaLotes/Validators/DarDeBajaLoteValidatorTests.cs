using EtbotFarma.Application.BajaLotes.Commands;
using EtbotFarma.Application.BajaLotes.Validators;

namespace EtbotFarma.Application.Tests.BajaLotes.Validators;

public class DarDeBajaLoteValidatorTests
{
    private readonly DarDeBajaLoteValidator _validator = new();

    [Fact]
    public void Con_datos_correctos_no_hay_errores()
    {
        Assert.True(_validator.Validate(new DarDeBajaLoteCommand(5, 40, "Próximo a caducar")).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void El_motivo_vacio_o_con_solo_espacios_es_obligatorio(string motivo)
    {
        var resultado = _validator.Validate(new DarDeBajaLoteCommand(5, 40, motivo));

        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "El motivo es obligatorio.");
    }

    [Fact]
    public void El_motivo_de_mas_de_200_caracteres_se_rechaza()
    {
        var resultado = _validator.Validate(new DarDeBajaLoteCommand(5, 40, new string('x', 201)));

        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "El motivo no puede tener más de 200 caracteres.");
    }

    [Fact]
    public void El_motivo_de_200_caracteres_se_acepta()
    {
        Assert.True(_validator.Validate(new DarDeBajaLoteCommand(5, 40, new string('x', 200))).IsValid);
    }

    [Fact]
    public void Lote_y_cantidad_deben_ser_mayores_que_cero()
    {
        var resultado = _validator.Validate(new DarDeBajaLoteCommand(0, 0, "Caducado"));

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(DarDeBajaLoteCommand.IdLote));
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(DarDeBajaLoteCommand.CantidadConsultada));
    }
}
