using EtbotFarma.Application.BitacoraMovimientos.Queries;
using FluentValidation;

namespace EtbotFarma.Application.BitacoraMovimientos.Validators;

public sealed class ConsultarBitacoraValidator : AbstractValidator<ConsultarBitacoraQuery>
{
    private static readonly string[] TiposPermitidos = ["ENTRADA", "SALIDA", "BAJA"];

    public ConsultarBitacoraValidator()
    {
        RuleFor(query => query.Desde)
            .Must((query, desde) => desde is null || query.Hasta is null || desde <= query.Hasta)
            .WithMessage("La fecha inicial no puede ser posterior a la fecha final.");

        RuleFor(query => query.Hasta)
            .Must(hasta => hasta != DateOnly.MaxValue)
            .WithMessage("La fecha final no es válida.");

        RuleFor(query => query.IdUsuario)
            .Must(id => id is null or > 0)
            .WithMessage("El usuario seleccionado no es válido.");

        RuleFor(query => query.IdMedicamento)
            .Must(id => id is null or > 0)
            .WithMessage("El medicamento seleccionado no es válido.");

        RuleFor(query => query.Tipo)
            .Must(tipo => string.IsNullOrWhiteSpace(tipo)
                || TiposPermitidos.Contains(tipo.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("El tipo debe ser ENTRADA, SALIDA o BAJA.");

        RuleFor(query => query.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser mayor que cero.");

        RuleFor(query => query.TamanoPagina)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}
