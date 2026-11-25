using EtbotFarma.Application.EdicionMedicamentos.Commands;
using FluentValidation;

namespace EtbotFarma.Application.EdicionMedicamentos.Validators;

/// <summary>
/// HU-09 escenario 7: valida que los campos descriptivos obligatorios no lleguen vacíos
/// antes de que se ejecute EditarMedicamentoHandler.
/// </summary>
public sealed class EditarMedicamentoValidator : AbstractValidator<EditarMedicamentoCommand>
{
    public EditarMedicamentoValidator()
    {
        RuleFor(x => x.IdMedicamento)
            .GreaterThan(0).WithMessage("El medicamento no existe.");

        RuleFor(x => x.NombreComercial)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre comercial no puede exceder 100 caracteres.");

        RuleFor(x => x.NombreSustancia)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(100).WithMessage("La sustancia activa no puede exceder 100 caracteres.");

        RuleFor(x => x.Presentacion)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(50).WithMessage("La presentación no puede exceder 50 caracteres.");

        RuleFor(x => x.Concentracion)
            .NotEmpty().WithMessage("Este campo es obligatorio.")
            .MaximumLength(50).WithMessage("La concentración no puede exceder 50 caracteres.");
    }
}
