namespace EtbotFarma.Domain.Enums;

/// <summary>
/// Indicador visual de caducidad en meses calendario:
/// Verde (> 12 meses), Amarillo (> 3 y <= 12 meses), Rojo (<= 3 meses).
/// </summary>
public enum SemaforoCaducidad
{
    Verde = 1,
    Amarillo = 2,
    Rojo = 3
}