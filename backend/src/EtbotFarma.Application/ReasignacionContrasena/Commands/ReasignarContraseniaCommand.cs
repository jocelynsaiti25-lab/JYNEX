using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.ReasignacionContrasena.Dtos;

namespace EtbotFarma.Application.ReasignacionContrasena.Commands;

/// <summary>
/// HU-05: pedido para establecer una contraseña nueva a una cuenta. Solo transporta los datos; no tiene lógica.
/// No pide la contraseña anterior (escenario 4): el Administrador la reasigna sin conocerla.
/// Validator: ReasignarContraseniaValidator · Handler: ReasignarContraseniaHandler
/// </summary>
public sealed record ReasignarContraseniaCommand(
    int IdUsuario,
    string NuevaContrasenia,
    string ConfirmarContrasenia) : ICommand<ContraseniaReasignadaDto>;
