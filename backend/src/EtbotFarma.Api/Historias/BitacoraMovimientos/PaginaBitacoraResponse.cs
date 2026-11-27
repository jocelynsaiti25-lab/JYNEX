namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

public sealed record PaginaBitacoraResponse(
    IReadOnlyList<MovimientoBitacoraResponse> Movimientos,
    int Pagina,
    int TamanoPagina,
    int Total,
    int TotalPaginas);
