using EtbotFarma.Application.Commands.InicioSesion;
using EtbotFarma.Application.Common.Validacion;

namespace EtbotFarma.Application.Validators.InicioSesion;

/// <summary>
/// HU-01 criterio 1: nombre de usuario y contraseña son obligatorios.
/// MediatR lo ejecuta automáticamente antes de IniciarSesionHandler (no se llama a mano).
/// </summary>
public class IniciarSesionValidator : IValidator<IniciarSesionCommand>
{
    public List<string> Validar(IniciarSesionCommand command)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(command.NombreUsuario))
        {
            errores.Add("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(command.Contrasenia))
        {
            errores.Add("La contraseña es obligatoria.");
        }

        return errores;
    }
}
