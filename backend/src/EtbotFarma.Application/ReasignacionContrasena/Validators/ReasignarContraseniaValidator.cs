using EtbotFarma.Application.Common.Validacion;
using EtbotFarma.Application.ReasignacionContrasena.Commands;
using FluentValidation;

namespace EtbotFarma.Application.ReasignacionContrasena.Validators;

/// <summary>
/// HU-05 escenarios 9, 10 y 11: campos obligatorios, misma política de contraseña que la HU-03
/// y que las dos contraseñas coincidan. MediatR lo ejecuta antes del handler (nadie lo crea con "new").
/// </summary>
public sealed class ReasignarContraseniaValidator : AbstractValidator<ReasignarContraseniaCommand>
{
    public ReasignarContraseniaValidator()
    {
        // Si una regla de un campo falla, ya no se revisan las siguientes de ESE campo
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(command => command.IdUsuario)
            .GreaterThan(0).WithMessage("La cuenta no existe");

        // Escenario 11 (obligatorio) y escenario 9 (política compartida con HU-03)
        RuleFor(command => command.NuevaContrasenia)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .ContraseniaSegura();

        // Escenarios 10 y 11
        RuleFor(command => command.ConfirmarContrasenia)
            .NotEmpty().WithMessage("La confirmación de la contraseña es obligatoria.")
            .Equal(command => command.NuevaContrasenia).WithMessage("Las contraseñas no coinciden");
    }
}
