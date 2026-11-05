using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.Features.Auth.IniciarSesion;

/// <summary>
/// HU-01: datos para iniciar sesión.
/// </summary>
public sealed record IniciarSesionCommand(string NombreUsuario, string Contrasenia)
    : ICommand<SesionIniciadaDto>;
