namespace EtbotFarma.Domain.ValueObjects;

public sealed record ExistenciasMedicamento(
    int Total,
    string SemaforoGeneral,
    bool EstaAgotado,
    bool EnStockBajo);
