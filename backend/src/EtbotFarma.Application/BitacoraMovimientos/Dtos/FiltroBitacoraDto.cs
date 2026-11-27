namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

public sealed record FiltroBitacoraDto(
    DateTime? DesdeUtc,
    DateTime? HastaUtcExclusivo,
    int? IdUsuario,
    int? IdMedicamento,
    string? Tipo);
