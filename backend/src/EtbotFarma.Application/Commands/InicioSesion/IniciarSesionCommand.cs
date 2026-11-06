using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.Dtos.InicioSesion;

namespace EtbotFarma.Application.Commands.InicioSesion;

/// <summary>
/// HU-01: datos para iniciar sesión.
/// </summary>
public sealed record IniciarSesionCommand(string NombreUsuario, string Contrasenia)
    : ICommand<IniciarSesionDto>;
