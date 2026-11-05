namespace EtbotFarma.Application.Common.Validacion;

/// <summary>
/// Contrato que cumple TODO validador del proyecto (sin librerías externas).
///
/// Cada historia crea su validador así:
///   public class CrearUsuarioValidator : IValidator&lt;CrearUsuarioCommand&gt;
///   {
///       public List&lt;string&gt; Validar(CrearUsuarioCommand command) { ... un if por regla ... }
///   }
///
/// No hay que llamarlo desde el handler: MediatR lo ejecuta solo
/// (ver Common/Behaviors/ValidationBehavior.cs).
/// </summary>
/// <typeparam name="T">El command o query que se valida.</typeparam>
public interface IValidator<in T>
{
    /// <summary>Devuelve la lista de errores. Vacía = datos correctos.</summary>
    List<string> Validar(T objeto);
}
