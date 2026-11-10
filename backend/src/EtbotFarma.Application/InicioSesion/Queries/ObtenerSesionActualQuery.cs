using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.InicioSesion.Dtos;

namespace EtbotFarma.Application.InicioSesion.Queries;

/// <summary>
/// HU-01 criterio 5: consulta los datos del usuario con sesión activa.
/// Angular la usa al recargar la página para saber si sigue habiendo sesión.
/// Handler: ObtenerSesionActualHandler
/// </summary>
public sealed record ObtenerSesionActualQuery : IQuery<UsuarioSesionDto>;
