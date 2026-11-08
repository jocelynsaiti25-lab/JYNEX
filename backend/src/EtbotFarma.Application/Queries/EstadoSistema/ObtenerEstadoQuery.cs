using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.Dtos.EstadoSistema;

namespace EtbotFarma.Application.Queries.EstadoSistema;

/// <summary>
/// Query de ejemplo: devuelve el estado de la API.
/// Sirve para comprobar que Angular → Api → Application funciona de punta a punta.
/// Es la PLANTILLA que seguirá cada historia de usuario.
/// </summary>
public sealed record ObtenerEstadoQuery : IQuery<EstadoSistemaDto>;
