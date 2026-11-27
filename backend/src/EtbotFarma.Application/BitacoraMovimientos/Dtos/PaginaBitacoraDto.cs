namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

/// <summary>Página del historial; la unidad paginada es el movimiento completo.</summary>
public sealed record PaginaBitacoraDto(
    IReadOnlyList<MovimientoBitacoraDto> Movimientos,
    int Pagina,
    int TamanoPagina,
    int TotalMovimientos,
    int TotalPaginas);
