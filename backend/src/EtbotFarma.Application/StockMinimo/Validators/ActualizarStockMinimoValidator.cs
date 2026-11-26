using EtbotFarma.Application.StockMinimo.Commands;
using FluentValidation;

namespace EtbotFarma.Application.StockMinimo.Validators;

/// <summary>
/// HU-11 escenario 1: el stock mínimo es obligatorio y debe ser un entero de 0 o más.
/// </summary>
public sealed class ActualizarStockMinimoValidator : AbstractValidator<ActualizarStockMinimoCommand>
{
    public ActualizarStockMinimoValidator()
    {
        RuleFor(x => x.IdMedicamento)
            .GreaterThan(0).WithMessage("El medicamento no existe.");

        RuleFor(x => x.NuevoStockMinimo)
            .NotNull().WithMessage("El stock mínimo es obligatorio.")
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo debe ser un número entero de 0 o más.");
    }
}
