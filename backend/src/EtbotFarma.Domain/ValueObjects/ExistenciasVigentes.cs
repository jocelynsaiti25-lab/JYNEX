namespace EtbotFarma.Domain.ValueObjects;

public sealed record ExistenciasVigentes(
    int Vigentes,
    int Caducadas,
    bool EstaAgotado,
    bool EnStockBajo)
{
    public bool TieneLotesCaducados => Caducadas > 0;
}
