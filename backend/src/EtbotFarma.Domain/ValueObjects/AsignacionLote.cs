namespace EtbotFarma.Domain.ValueObjects;

public sealed record AsignacionLote(int IdLote, string NumeroLote, DateOnly FechaCaducidad, int Cantidad);
