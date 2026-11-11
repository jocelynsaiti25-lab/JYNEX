using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.CierreSesion.Commands;

/// <summary>
/// HU-02: pedido para cerrar la sesión actual. No lleva datos: el token viene en la cookie
/// y lo lee el handler con ICookieSesionService (el controller nunca toca cookies).
/// Es un command porque MODIFICA datos: guarda la fecha de cierre de la sesión.
/// Devuelve true si había una sesión abierta y se cerró; false si ya no había sesión.
/// Handler: CerrarSesionHandler (no necesita validator: no recibe datos del usuario).
/// </summary>
public sealed record CerrarSesionCommand : ICommand<bool>;
