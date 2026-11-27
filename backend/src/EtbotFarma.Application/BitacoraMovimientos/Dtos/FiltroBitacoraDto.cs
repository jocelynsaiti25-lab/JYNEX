namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

/// <summary>Filtros ya normalizados para consultar la bitácora. Las fechas son límites UTC.</summary>
public sealed record FiltroBitacoraDto(
    DateTime? DesdeUtc,
    DateTime? HastaUtcExclusivo,
    int? IdUsuario,
    int? IdMedicamento,
    string? Tipo,
    int Pagina,
    int TamanoPagina);
