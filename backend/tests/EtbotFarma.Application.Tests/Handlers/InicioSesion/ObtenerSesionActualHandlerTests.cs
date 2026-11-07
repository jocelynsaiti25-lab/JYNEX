using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Handlers.InicioSesion;
using EtbotFarma.Application.Queries.InicioSesion;
using EtbotFarma.Application.Tests.Falsos;

namespace EtbotFarma.Application.Tests.Handlers.InicioSesion;

/// <summary>Pruebas de HU-01: consultar la sesión actual.</summary>
public class ObtenerSesionActualHandlerTests
{
    private readonly UsuarioActualFalso _usuarioActual = new();
    private readonly ObtenerSesionActualHandler _handler;

    public ObtenerSesionActualHandlerTests()
    {
        _handler = new ObtenerSesionActualHandler(_usuarioActual);
    }

    [Fact]
    public async Task Con_sesion_devuelve_los_datos_del_usuario()
    {
        _usuarioActual.IdUsuario = 7;
        _usuarioActual.NombreUsuario = "ana";
        _usuarioActual.NombreCompleto = "Ana López";
        _usuarioActual.Rol = "Empleado";

        var usuario = await _handler.Handle(new ObtenerSesionActualQuery(), CancellationToken.None);

        Assert.Equal(7, usuario.IdUsuario);
        Assert.Equal("Ana López", usuario.NombreCompleto);
        Assert.Equal("Empleado", usuario.Rol);
    }

    [Fact]
    public async Task Sin_sesion_lanza_NoAutenticadoException()
    {
        await Assert.ThrowsAsync<NoAutenticadoException>(
            () => _handler.Handle(new ObtenerSesionActualQuery(), CancellationToken.None));
    }
}
