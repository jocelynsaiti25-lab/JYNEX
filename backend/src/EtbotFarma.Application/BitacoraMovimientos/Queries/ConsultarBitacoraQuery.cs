using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Queries;

/// <summary>Filtros de consulta de bitácora. Las fechas representan días en la zona operativa.</summary>
public sealed record ConsultarBitacoraQuery(
    DateOnly? Desde,
    DateOnly? Hasta,
    int? IdUsuario,
    int? IdMedicamento,
    string? Tipo,
    int Pagina = 1,
    int TamanoPagina = 20) : IQuery<PaginaBitacoraDto>;
