using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.ReasignacionContrasena.Dtos;

namespace EtbotFarma.Application.ReasignacionContrasena.Queries;

/// <summary>
/// HU-05: datos de la cuenta elegida en la consulta de usuarios (HU-04), para mostrarlos en el
/// formulario y en la confirmación (escenario 2). Solo LEE. Handler: ObtenerCuentaHandler
/// </summary>
public sealed record ObtenerCuentaQuery(int IdUsuario) : IQuery<CuentaDto>;
