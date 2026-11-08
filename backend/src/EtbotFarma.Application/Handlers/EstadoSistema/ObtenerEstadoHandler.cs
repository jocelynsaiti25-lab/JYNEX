using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.Dtos.EstadoSistema;
using EtbotFarma.Application.Queries.EstadoSistema;

namespace EtbotFarma.Application.Handlers.EstadoSistema;

/// <summary>
/// Handler: contiene la lógica del caso de uso.
/// Recibe sus dependencias por constructor (inyección de dependencias).
/// </summary>
public sealed class ObtenerEstadoHandler(IDateTimeProvider dateTimeProvider)
    : IQueryHandler<ObtenerEstadoQuery, EstadoSistemaDto>
{
    public const string NombreAplicacion = "ETBOTFARMA WEB";
    public const string Version = "1.0.0";

    public Task<EstadoSistemaDto> Handle(ObtenerEstadoQuery request, CancellationToken cancellationToken)
    {
        var estado = new EstadoSistemaDto(NombreAplicacion, Version, dateTimeProvider.UtcNow);
        return Task.FromResult(estado);
    }
}
