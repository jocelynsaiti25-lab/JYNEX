using MediatR;

namespace EtbotFarma.Application.Common.Messaging;

/// <summary>
/// COMMAND (CQRS): operación que MODIFICA datos.
/// Ejemplos: IniciarSesionCommand, RegistrarSalidaCommand, DarBajaLoteCommand.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>Handler que ejecuta un command.</summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
