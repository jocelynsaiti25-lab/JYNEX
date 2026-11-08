using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Falsos;

/// <summary>Usuario actual configurable para las pruebas.</summary>
public sealed class UsuarioActualFalso : IUsuarioActual
{
    public int? IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}
