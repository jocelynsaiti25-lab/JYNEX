using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.Dtos.InicioSesion;

namespace EtbotFarma.Application.Queries.InicioSesion;

/// <summary>
/// HU-01 criterio 5: consulta los datos del usuario con sesión activa.
/// Angular la usa al recargar la página para saber si sigue habiendo sesión.
/// Handler: ObtenerSesionActualHandler
/// </summary>
public sealed record ObtenerSesionActualQuery : IQuery<UsuarioSesionDto>;
