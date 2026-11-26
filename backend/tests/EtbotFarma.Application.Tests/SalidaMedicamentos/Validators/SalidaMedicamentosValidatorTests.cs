using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using EtbotFarma.Application.SalidaMedicamentos.Validators;

namespace EtbotFarma.Application.Tests.SalidaMedicamentos.Validators;

public class SalidaMedicamentosValidatorTests
{
    [Theory]
    [InlineData(0, 5, "IdMedicamento")]
    [InlineData(-1, 5, "IdMedicamento")]
    [InlineData(1, 0, "CantidadSolicitada")]
    [InlineData(1, -4, "CantidadSolicitada")]
    public void La_propuesta_rechaza_medicamento_o_cantidad_invalidos(int idMedicamento, int cantidad, string campo)
    {
        var resultado = new ProponerSalidaMedicamentoValidator()
            .Validate(new ProponerSalidaMedicamentoQuery(idMedicamento, cantidad));

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    [Fact]
    public void La_propuesta_con_datos_correctos_es_valida()
    {
        Assert.True(new ProponerSalidaMedicamentoValidator().Validate(new ProponerSalidaMedicamentoQuery(1, 5)).IsValid);
    }

    [Theory]
    [InlineData(0, 5, "IdMedicamento")]
    [InlineData(1, 0, "Cantidad")]
    [InlineData(1, -2, "Cantidad")]
    public void El_registro_rechaza_medicamento_o_cantidad_invalidos(int idMedicamento, int cantidad, string campo)
    {
        var resultado = new RegistrarSalidaMedicamentoValidator()
            .Validate(new RegistrarSalidaMedicamentoCommand(idMedicamento, cantidad));

        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }
}
