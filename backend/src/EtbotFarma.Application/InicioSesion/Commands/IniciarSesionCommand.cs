using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.InicioSesion.Dtos;

namespace EtbotFarma.Application.InicioSesion.Commands;

/// <summary>
/// HU-01: pedido para iniciar sesión. Solo transporta los datos; no tiene lógica.
/// Validator: IniciarSesionValidator · Handler: IniciarSesionHandler
/// </summary>
public sealed record IniciarSesionCommand(string NombreUsuario, string Contrasenia)
    : ICommand<UsuarioSesionDto>;
