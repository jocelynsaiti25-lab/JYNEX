namespace EtbotFarma.Api.Historias.BitacoraMovimientos;

/// <summary>Contrato HTTP de la página de movimientos de la bitácora.</summary>
public sealed record PaginaBitacoraResponse(
    IReadOnlyList<MovimientoBitacoraResponse> Movimientos,
    int Pagina,
    int TamanoPagina,
    int TotalMovimientos,
    int TotalPaginas);
