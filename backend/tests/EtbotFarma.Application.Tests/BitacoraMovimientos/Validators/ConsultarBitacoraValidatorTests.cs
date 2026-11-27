using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.BitacoraMovimientos.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.BitacoraMovimientos.Validators;

public class ConsultarBitacoraValidatorTests
{
    private readonly ConsultarBitacoraValidator _validador = new();

    private static ConsultarBitacoraQuery Consulta(
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? idUsuario = null,
        int? idMedicamento = null,
        string? tipo = null,
        int pagina = 1,
        int tamanoPagina = 20) =>
        new(desde, hasta, idUsuario, idMedicamento, tipo, pagina, tamanoPagina);

    [Fact]
    public void Consulta_sin_filtros_es_valida() =>
        _validador.TestValidate(Consulta()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Consulta_con_todos_los_filtros_combinados_es_valida() =>
        _validador
            .TestValidate(Consulta(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 2, 5, "BAJA", 3, 50))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fecha_inicial_igual_a_la_final_es_valida() =>
        _validador
            .TestValidate(Consulta(new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 10)))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Fecha_inicial_posterior_a_la_final_marca_rango_no_valido() =>
        _validador
            .TestValidate(Consulta(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 10)))
            .ShouldHaveAnyValidationError()
            .WithErrorMessage(ConsultarBitacoraValidator.MensajeRangoNoValido);

    [Theory]
    [InlineData(1999, 12, 31)]
    [InlineData(2101, 1, 1)]
    public void Fecha_fuera_del_rango_permitido_marca_rango_no_valido(int anio, int mes, int dia)
    {
        var fecha = new DateOnly(anio, mes, dia);

        _validador.TestValidate(Consulta(desde: fecha))
            .ShouldHaveValidationErrorFor(c => c.Desde)
            .WithErrorMessage(ConsultarBitacoraValidator.MensajeRangoNoValido);
        _validador.TestValidate(Consulta(hasta: fecha))
            .ShouldHaveValidationErrorFor(c => c.Hasta)
            .WithErrorMessage(ConsultarBitacoraValidator.MensajeRangoNoValido);
    }

    [Theory]
    [InlineData("ENTRADA")]
    [InlineData("SALIDA")]
    [InlineData("BAJA")]
    [InlineData("baja")]
    [InlineData(" Salida ")]
    [InlineData(null)]
    [InlineData("")]
    public void Tipo_permitido_o_vacio_es_valido(string? tipo) =>
        _validador.TestValidate(Consulta(tipo: tipo)).ShouldNotHaveValidationErrorFor(c => c.Tipo);

    [Theory]
    [InlineData("TRASLADO")]
    [InlineData("DROP TABLE movimiento")]
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Pagina_menor_que_uno_se_rechaza(int pagina) =>
        _validador.TestValidate(Consulta(pagina: pagina)).ShouldHaveValidationErrorFor(c => c.Pagina);

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Tamano_de_pagina_fuera_de_limites_se_rechaza(int tamano) =>
        _validador.TestValidate(Consulta(tamanoPagina: tamano)).ShouldHaveValidationErrorFor(c => c.TamanoPagina);

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Tamano_de_pagina_en_los_limites_es_valido(int tamano) =>
        _validador.TestValidate(Consulta(tamanoPagina: tamano)).ShouldNotHaveValidationErrorFor(c => c.TamanoPagina);
}
