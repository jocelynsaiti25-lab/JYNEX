using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.ConsultaUsuarios.Dtos;
using EtbotFarma.Application.ConsultaUsuarios.Queries;

namespace EtbotFarma.Application.ConsultaUsuarios.Handlers;

/// <summary>
/// HU-04: consulta y busca las cuentas registradas.
/// Es SOLO LECTURA (escenario 14): no llama a GuardarCambiosAsync ni modifica nada.
/// Solo el Administrador llega aquí: el controller tiene [Authorize(Roles = "Administrador")] (escenario 3).
/// </summary>
public sealed class ConsultarUsuariosHandler(IUsuarioRepository usuarios)
    : IQueryHandler<ConsultarUsuariosQuery, IReadOnlyList<UsuarioRegistradoDto>>
{
    public async Task<IReadOnlyList<UsuarioRegistradoDto>> Handle(ConsultarUsuariosQuery query, CancellationToken cancellationToken)
    {
        // 1. Escenario 10: se quitan los espacios del inicio y del final (" MFLopez " → "MFLopez").
        //    Escenario 6: si queda vacío, no hay filtro y se listan todas las cuentas.
        var busqueda = string.IsNullOrWhiteSpace(query.Busqueda) ? null : query.Busqueda.Trim();

        // 2. Obtener las cuentas (la búsqueda parcial y sin mayúsculas la hace el repositorio, escenario 4)
        var lista = await usuarios.ListarAsync(busqueda, cancellationToken);

        // 3. Convertir a Dto: solo los datos que se muestran, NUNCA la contraseña ni su hash (escenario 7)
        return lista
            .Select(usuario => new UsuarioRegistradoDto(
                usuario.Id,
                usuario.NombreCompleto,
                usuario.NombreUsuario,
                usuario.Correo,
                usuario.Telefono,
                usuario.Rol.Nombre))
            .ToList();
    }
}
