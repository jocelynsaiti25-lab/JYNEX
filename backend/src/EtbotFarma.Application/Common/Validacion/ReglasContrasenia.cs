using FluentValidation;

namespace EtbotFarma.Application.Common.Validacion;

/// <summary>
/// Política de contraseñas del sistema, COMPARTIDA por las historias que reciben una contraseña:
/// HU-03 (crear usuario) y HU-05 (reasignar contraseña). Así, si la política cambia, se cambia aquí una sola vez.
///   De 8 a 100 caracteres, con al menos una mayúscula, una minúscula y un número.
///
/// Uso dentro de un validator:
///   RuleFor(c => c.Contrasenia).NotEmpty().WithMessage("...").ContraseniaSegura();
/// </summary>
public static class ReglasContrasenia
{
    public static IRuleBuilderOptions<T, string> ContraseniaSegura<T>(this IRuleBuilder<T, string> regla)
        => regla
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede tener más de 100 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número.");
}
