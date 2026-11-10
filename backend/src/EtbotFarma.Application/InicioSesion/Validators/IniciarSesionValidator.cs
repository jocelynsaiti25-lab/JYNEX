using EtbotFarma.Application.InicioSesion.Commands;
using FluentValidation;

namespace EtbotFarma.Application.InicioSesion.Validators;

/// <summary>
/// HU-01 criterio 1: nombre de usuario y contraseña son obligatorios.
/// MediatR lo ejecuta automáticamente antes de IniciarSesionHandler (nadie lo crea con "new").
/// </summary>
public sealed class IniciarSesionValidator : AbstractValidator<IniciarSesionCommand>
{
    public IniciarSesionValidator()
    {
        RuleFor(command => command.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

        RuleFor(command => command.Contrasenia)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
