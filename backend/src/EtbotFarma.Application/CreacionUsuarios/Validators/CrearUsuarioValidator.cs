using EtbotFarma.Application.CreacionUsuarios.Commands;
using EtbotFarma.Domain.Entities;
using FluentValidation;

namespace EtbotFarma.Application.CreacionUsuarios.Validators;

/// <summary>
/// HU-03 criterios 2 y 3: datos obligatorios, formato de correo y teléfono, y contraseña segura.
/// MediatR lo ejecuta automáticamente antes de CrearUsuarioHandler (nadie lo crea con "new").
/// Aquí solo se revisan los DATOS que llegan; lo que necesita la base de datos
/// (usuario o correo repetidos) lo revisa el handler.
/// </summary>
public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioCommand>
{
    public CrearUsuarioValidator()
    {
        // Si una regla de un campo falla, ya no se revisan las siguientes de ESE campo
        // (así no aparece "obligatorio" y "formato inválido" al mismo tiempo).
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(command => command.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre no puede tener más de 50 caracteres.");

        RuleFor(command => command.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(80).WithMessage("Los apellidos no pueden tener más de 80 caracteres.");

        // Con el nombre de usuario se inicia sesión: sin espacios, fácil de escribir
        RuleFor(command => command.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .Length(3, 50).WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres.")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("El nombre de usuario solo puede tener letras, números, punto, guion y guion bajo (sin espacios).");

        // HU-03 escenario 8: algo@dominio.algo, sin espacios
        RuleFor(command => command.Correo)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(100).WithMessage("El correo no puede tener más de 100 caracteres.")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Formato de correo inválido.");

        // HU-03 criterio 3: formato de teléfono (10 dígitos)
        RuleFor(command => command.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Matches(@"^\d{10}$").WithMessage("Formato de teléfono inválido: escribe 10 dígitos, sin espacios ni guiones.");

        // HU-03 criterio 3: seguridad de la contraseña
        RuleFor(command => command.Contrasenia)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede tener más de 100 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número.");

        // HU-03 criterio 2: solo existen dos roles
        RuleFor(command => command.IdRol)
            .Must(idRol => idRol is Rol.Administrador or Rol.Empleado)
            .WithMessage("Selecciona un rol válido (Administrador o Empleado).");
    }
}
