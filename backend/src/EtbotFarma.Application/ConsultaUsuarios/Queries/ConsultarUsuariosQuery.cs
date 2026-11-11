using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.ConsultaUsuarios.Dtos;

namespace EtbotFarma.Application.ConsultaUsuarios.Queries;

/// <summary>
/// HU-04: pedido para consultar las cuentas registradas. Es una QUERY porque solo LEE (escenario 14).
/// Busqueda: parte del nombre de usuario (escenario 4). Vacía o null = todas las cuentas (escenario 6).
/// Validator: ConsultarUsuariosValidator · Handler: ConsultarUsuariosHandler
/// </summary>
public sealed record ConsultarUsuariosQuery(string? Busqueda) : IQuery<IReadOnlyList<UsuarioRegistradoDto>>;
