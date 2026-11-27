namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

public sealed record PaginaBitacoraDto(
    IReadOnlyList<MovimientoBitacoraDto> Movimientos,
    int Pagina,
    int TamanoPagina,
    int Total,
    int TotalPaginas);
