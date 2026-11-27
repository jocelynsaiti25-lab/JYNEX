namespace EtbotFarma.Api.Historias.BajaLotes;

public sealed record DarDeBajaLoteResponse(
    int IdLote,
    int IdMedicamento,
    string NombreMedicamento,
    string NumeroLote,
    string FechaCaducidad,
    int CantidadRetirada,
    string Motivo,
    string Mensaje);
