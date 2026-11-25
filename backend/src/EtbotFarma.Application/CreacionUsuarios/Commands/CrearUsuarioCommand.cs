using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.CreacionUsuarios.Dtos;

namespace EtbotFarma.Application.CreacionUsuarios.Commands;

/// <summary>
/// HU-03: pedido para crear la cuenta de una persona del personal. Solo transporta los datos; no tiene lógica.
/// Validator: CrearUsuarioValidator · Handler: CrearUsuarioHandler
/// </summary>
public sealed record CrearUsuarioCommand(
    string Nombre,
    string Apellidos,
    string NombreUsuario,
    string Correo,
    string Telefono,
    string Contrasenia,
    int IdRol) : ICommand<UsuarioCreadoDto>;
