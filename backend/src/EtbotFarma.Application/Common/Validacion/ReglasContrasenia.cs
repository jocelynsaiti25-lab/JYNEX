using FluentValidation;

namespace EtbotFarma.Application.Common.Validacion;

/// <summary>
/// Política de contraseñas del sistema, COMPARTIDA por las historias que reciben una contraseña:
/// HU-03 (crear usuario) y HU-05 (reasignar contraseña). Así, si la política cambia, se cambia aquí una sola vez.
///   De 8 a 15 caracteres, con al menos una mayúscula, una minúscula, un número
///   y un carácter especial (por ejemplo @, #, $, %, ! o &).
///
/// El frontend repite estas mismas reglas y mensajes en frontend/src/app/shared/validators/contrasenia.ts
/// (si cambias algo aquí, cámbialo también allá).
///
/// Ojo: los 15 caracteres son de la contraseña que escribe el usuario, NO del hash.
/// En la base se guarda solo el hash (columna contrasenia_hash VARCHAR(255)).
///
/// Uso dentro de un validator:
///   RuleFor(c => c.Contrasenia).NotEmpty().WithMessage("...").ContraseniaSegura();
/// </summary>
public static class ReglasContrasenia
{
    public const int LongitudMinima = 8;
    public const int LongitudMaxima = 15;

    /// <summary>
    /// Carácter especial = cualquier cosa que NO sea letra, número ni espacio (@, #, $, %, !, &, *, -, _ ...).
    /// \p{L} = cualquier letra (incluye ñ y acentos) · \p{N} = cualquier número · \s = espacio.
    /// </summary>
    public const string PatronCaracterEspecial = @"[^\p{L}\p{N}\s]";

    public static IRuleBuilderOptions<T, string> ContraseniaSegura<T>(this IRuleBuilder<T, string> regla)
        => regla
            .MinimumLength(LongitudMinima).WithMessage($"La contraseña debe tener al menos {LongitudMinima} caracteres.")
            .MaximumLength(LongitudMaxima).WithMessage($"La contraseña no puede tener más de {LongitudMaxima} caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número.")
            .Matches(PatronCaracterEspecial)
            .WithMessage("La contraseña debe tener al menos un carácter especial (por ejemplo @, #, $, %, ! o &).");
}
