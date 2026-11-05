namespace EtbotFarma.Application.Features.Sesiones.HU01_IniciarSesion;

/// <summary>
/// HU-01 criterio 1: nombre de usuario y contraseña son obligatorios.
/// </summary>
public class IniciarSesionValidator
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
