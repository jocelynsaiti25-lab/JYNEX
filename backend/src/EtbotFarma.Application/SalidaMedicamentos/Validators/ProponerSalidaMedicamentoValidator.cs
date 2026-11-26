using EtbotFarma.Application.SalidaMedicamentos.Queries;
using FluentValidation;

namespace EtbotFarma.Application.SalidaMedicamentos.Validators;

public sealed class ProponerSalidaMedicamentoValidator : AbstractValidator<ProponerSalidaMedicamentoQuery>
{
    public ProponerSalidaMedicamentoValidator()
    {
        RuleFor(x => x.IdMedicamento)
            .GreaterThan(0)
            .WithMessage("Debe seleccionar un medicamento válido.");

        RuleFor(x => x.CantidadSolicitada)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser un número entero mayor que cero.");
    }
}
