using EtbotFarma.Application.BajaLotes.Commands;
using EtbotFarma.Domain.Entities;
using FluentValidation;

namespace EtbotFarma.Application.BajaLotes.Validators;

public sealed class DarDeBajaLoteValidator : AbstractValidator<DarDeBajaLoteCommand>
{
    public DarDeBajaLoteValidator()
    {
        RuleFor(x => x.IdLote)
            .GreaterThan(0)
            .WithMessage("Debe indicar un lote válido.");

        RuleFor(x => x.CantidadConsultada)
            .GreaterThan(0)
            .WithMessage("La cantidad a retirar debe ser mayor que cero.");

        RuleFor(x => x.Motivo)
            .Must(motivo => !string.IsNullOrWhiteSpace(motivo))
            .WithMessage("El motivo es obligatorio.")
            .Must(motivo => (motivo?.Trim().Length ?? 0) <= Movimiento.LongitudMaximaMotivo)
            .WithMessage("El motivo no puede tener más de 200 caracteres.");
    }
}
