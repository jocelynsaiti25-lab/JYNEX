using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.BitacoraMovimientos.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.BitacoraMovimientos.Validators;

public class ExportarBitacoraValidatorTests
{
    private readonly ExportarBitacoraValidator _validador = new();

    private static ExportarBitacoraQuery Consulta(
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? idUsuario = null,
        int? idMedicamento = null,
        string? tipo = null) =>
        new(desde, hasta, idUsuario, idMedicamento, tipo);

    [Fact]
    public void Exportacion_sin_filtros_es_valida() =>
        _validador.TestValidate(Consulta()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Exportacion_con_todos_los_filtros_es_valida() =>
        _validador
            .TestValidate(Consulta(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 2, 5, "BAJA"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fecha_inicial_posterior_a_la_final_marca_rango_no_valido()
    {
        var resultado = _validador.TestValidate(Consulta(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 10)));

        Assert.Contains(resultado.Errors, e => e.ErrorMessage == ConsultarBitacoraValidator.MensajeRangoNoValido);
    }

    [Theory]
    [InlineData(1999, 12, 31)]
    [InlineData(2101, 1, 1)]
    public void Fecha_fuera_del_rango_permitido_se_rechaza(int anio, int mes, int dia)
    {
        var fecha = new DateOnly(anio, mes, dia);

        _validador.TestValidate(Consulta(desde: fecha)).ShouldHaveValidationErrorFor(c => c.Desde);
        _validador.TestValidate(Consulta(hasta: fecha)).ShouldHaveValidationErrorFor(c => c.Hasta);
    }

    [Theory]
    [InlineData("TRASLADO")]
    [InlineData("=1+1")]
    public void Tipo_desconocido_se_rechaza(string tipo) =>
        _validador.TestValidate(Consulta(tipo: tipo)).ShouldHaveValidationErrorFor(c => c.Tipo);

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Ids_no_positivos_se_rechazan(int id)
    {
        _validador.TestValidate(Consulta(idUsuario: id)).ShouldHaveValidationErrorFor(c => c.IdUsuario);
        _validador.TestValidate(Consulta(idMedicamento: id)).ShouldHaveValidationErrorFor(c => c.IdMedicamento);
    }
}
