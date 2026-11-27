using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Queries;

public sealed record ConsultarBitacoraQuery(
    DateOnly? Desde,
    DateOnly? Hasta,
    int? IdUsuario,
    int? IdMedicamento,
    string? Tipo,
    int Pagina,
    int TamanoPagina) : IQuery<PaginaBitacoraDto>;
