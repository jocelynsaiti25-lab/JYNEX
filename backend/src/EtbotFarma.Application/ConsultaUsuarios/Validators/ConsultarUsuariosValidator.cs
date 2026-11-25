using EtbotFarma.Application.ConsultaUsuarios.Queries;
using FluentValidation;

namespace EtbotFarma.Application.ConsultaUsuarios.Validators;

/// <summary>
/// HU-04: la búsqueda es opcional, pero no puede ser más larga que un nombre de usuario
/// (usuario.nombre_usuario es VARCHAR(50) en MySQL). MediatR lo ejecuta antes del handler.
/// </summary>
public sealed class ConsultarUsuariosValidator : AbstractValidator<ConsultarUsuariosQuery>
{
    public ConsultarUsuariosValidator()
    {
        RuleFor(query => query.Busqueda)
            .MaximumLength(50).WithMessage("La búsqueda no puede tener más de 50 caracteres.");
    }
}
