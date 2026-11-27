using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.BitacoraMovimientos.Validators;
using FluentValidation.TestHelper;

namespace EtbotFarma.Application.Tests.BitacoraMovimientos.Validators;

public class ConsultarBitacoraValidatorTests
{
    private readonly ConsultarBitacoraValidator _validador = new();

    [Fact]
    public void Acepta_filtros_opcionales_validos_y_tipo_sin_importar_mayusculas()
    {
        var resultado = _validador.TestValidate(new ConsultarBitacoraQuery(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 2, 5, "entrada", 1, 100));

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rechaza_rango_invertido_identificadores_no_positivos_tipo_desconocido_y_paginacion_invalida()
    {
        var resultado = _validador.TestValidate(new ConsultarBitacoraQuery(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 31), 0, -1, "AJUSTE", 0, 101));

        resultado.ShouldHaveValidationErrorFor(query => query.Desde);
        resultado.ShouldHaveValidationErrorFor(query => query.IdUsuario);
        resultado.ShouldHaveValidationErrorFor(query => query.IdMedicamento);
        resultado.ShouldHaveValidationErrorFor(query => query.Tipo);
        resultado.ShouldHaveValidationErrorFor(query => query.Pagina);
        resultado.ShouldHaveValidationErrorFor(query => query.TamanoPagina);
    }

    [Fact]
    public void Rechaza_fecha_final_que_no_permite_calcular_el_limite_exclusivo()
    {
        var resultado = _validador.TestValidate(new ConsultarBitacoraQuery(
            null, DateOnly.MaxValue, null, null, null));

        resultado.ShouldHaveValidationErrorFor(query => query.Hasta);
    }
}
