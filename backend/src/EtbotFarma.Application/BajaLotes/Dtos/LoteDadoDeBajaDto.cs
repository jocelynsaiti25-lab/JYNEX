namespace EtbotFarma.Application.BajaLotes.Dtos;

public sealed record LoteDadoDeBajaDto(
    int IdLote,
    int IdMedicamento,
    string NombreMedicamento,
    string NumeroLote,
    DateOnly FechaCaducidad,
    int CantidadRetirada,
    string Motivo,
    string Mensaje);
