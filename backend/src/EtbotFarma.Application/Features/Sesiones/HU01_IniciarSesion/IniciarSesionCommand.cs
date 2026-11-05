using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.Features.Sesiones.HU01_IniciarSesion;

/// <summary>
/// HU-01: datos para iniciar sesión.
/// </summary>
public sealed record IniciarSesionCommand(string NombreUsuario, string Contrasenia)
    : ICommand<IniciarSesionDto>;
