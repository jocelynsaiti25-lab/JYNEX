
using EtbotFarma.Application.SalidaMedicamentos.Commands;
using FluentValidation;

namespace EtbotFarma.Application.SalidaMedicamentos.Validators;

/// <summary>
/// HU-13: valida los datos necesarios para registrar una salida de medicamentos.
/// </summary>
public sealed class RegistrarSalidaMedicamentoValidator
    : AbstractValidator<RegistrarSalidaMedicamentoCommand>
{
    public RegistrarSalidaMedicamentoValidator()
    {
        RuleFor(x => x.IdMedicamento)
            .GreaterThan(0)
            .WithMessage("Debe seleccionar un medicamento válido.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser un número entero mayor que cero.");
    }
}

