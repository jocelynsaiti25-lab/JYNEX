using MediatR;

namespace EtbotFarma.Application.Common.Messaging;

/// <summary>
/// QUERY (CQRS): operación que SOLO LEE datos, nunca los modifica.
/// Ejemplos: BuscarInventarioQuery, ConsultarBitacoraQuery.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>Handler que ejecuta una query.</summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
