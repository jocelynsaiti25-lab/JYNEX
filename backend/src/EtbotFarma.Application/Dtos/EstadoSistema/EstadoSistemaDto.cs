namespace EtbotFarma.Application.Dtos.EstadoSistema;

/// <summary>Datos que se devuelven al frontend.</summary>
public sealed record EstadoSistemaDto(
    string Aplicacion,
    string Version,
    DateTimeOffset FechaServidorUtc);
