namespace EtbotFarma.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio (Usuario, Medicamento, Lote, etc.).
/// Toda entidad tiene un identificador único.
/// </summary>
/// <typeparam name="TId">Tipo del identificador (int, Guid...).</typeparam>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
}
