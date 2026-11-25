namespace EtbotFarma.Domain.Enums;

/// <summary>
/// Clasificación preventiva por meses calendario hasta la fecha de caducidad:
/// Verde (> 12 meses), Amarillo (> 3 y <= 12 meses), Rojo (<= 3 meses, incluye caducados).
/// </summary>
public enum SemaforoCaducidad
{
    Verde = 1,
    Amarillo = 2,
    Rojo = 3
}