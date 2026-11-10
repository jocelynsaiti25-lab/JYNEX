using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.InicioSesion.Dtos;

namespace EtbotFarma.Application.InicioSesion.Commands;

/// <summary>
/// HU-01 criterios 5 y 6: revisa en cada petición si la sesión de la cookie sigue vigente.
/// Es un command (no una query) porque actualiza la última actividad o cierra la sesión.
/// Devuelve null si la sesión no es válida. Handler: ValidarSesionHandler
/// </summary>
public sealed record ValidarSesionCommand(string Token) : ICommand<UsuarioSesionDto?>;
