using EtbotFarma.Application.CierreSesion.Commands;
using EtbotFarma.Application.CierreSesion.Handlers;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.CierreSesion.Handlers;

/// <summary>Pruebas de HU-02: cerrar la sesión del lado del servidor.</summary>
public class CerrarSesionHandlerTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly SesionRepositoryFalso _sesiones = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly RelojFalso _reloj = new(Inicio);
    private readonly CookieSesionServiceFalso _cookie = new();
    private readonly Sesion _sesion;
    private readonly CerrarSesionHandler _handler;

    public CerrarSesionHandlerTests()
    {
        var usuario = Usuario.Crear("Ana", "López", "ana", "ana@etbotfarma.com", "hash", "5512345678",
            Rol.Reconstruir(Rol.Empleado, "Empleado"), Inicio.UtcDateTime);

        // TokenSesionServiceFalso calcula el "hash" como "h:" + token
        _sesion = Sesion.Iniciar(usuario, "h:token-123", Inicio.UtcDateTime, TimeSpan.FromMinutes(20));
        _sesiones.Sesiones.Add(_sesion);

        _handler = new CerrarSesionHandler(_cookie, _sesiones, new TokenSesionServiceFalso(), _unitOfWork, _reloj);
    }

    /// <summary>Escenario 1: la sesión se invalida EN EL SERVIDOR (no solo en el navegador).</summary>
    [Fact]
    public async Task Con_sesion_abierta_guarda_la_fecha_de_cierre_y_borra_la_cookie()
    {
        _cookie.TokenEnLaPeticion = "token-123";
        _reloj.UtcNow = Inicio.AddMinutes(5);

        var cerrada = await _handler.Handle(new CerrarSesionCommand(), CancellationToken.None);

        Assert.True(cerrada);
        Assert.True(_sesion.Cerrada);
        Assert.Equal(Inicio.AddMinutes(5).UtcDateTime, _sesion.FechaCierre);
        Assert.False(_sesion.EstaVigente(Inicio.AddMinutes(6).UtcDateTime));   // la cookie anterior ya no sirve
        Assert.Equal(1, _sesiones.VecesActualizada);
        Assert.Equal(1, _unitOfWork.VecesGuardado);
        Assert.True(_cookie.Eliminada);
    }

    /// <summary>Escenario 10: la pantalla quedó abierta pero ya no hay cookie → sin error.</summary>
    [Fact]
    public async Task Sin_cookie_no_falla_y_borra_la_cookie()
    {
        _cookie.TokenEnLaPeticion = null;

        var cerrada = await _handler.Handle(new CerrarSesionCommand(), CancellationToken.None);

        Assert.False(cerrada);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
        Assert.True(_cookie.Eliminada);
    }

    /// <summary>Escenario 10: la sesión ya estaba cerrada (por inactividad) → sin error y sin volver a guardar.</summary>
    [Fact]
    public async Task Con_sesion_ya_cerrada_no_falla_ni_cambia_la_fecha_de_cierre()
    {
        _sesion.Cerrar(Inicio.AddMinutes(20).UtcDateTime);
        _cookie.TokenEnLaPeticion = "token-123";
        _reloj.UtcNow = Inicio.AddMinutes(40);

        var cerrada = await _handler.Handle(new CerrarSesionCommand(), CancellationToken.None);

        Assert.False(cerrada);
        Assert.Equal(Inicio.AddMinutes(20).UtcDateTime, _sesion.FechaCierre);   // conserva la fecha original
        Assert.Equal(0, _unitOfWork.VecesGuardado);
        Assert.True(_cookie.Eliminada);
    }

    /// <summary>Un token que no existe en la base (por ejemplo, de una base reiniciada) no causa error.</summary>
    [Fact]
    public async Task Con_token_desconocido_no_falla_y_borra_la_cookie()
    {
        _cookie.TokenEnLaPeticion = "token-que-no-existe";

        var cerrada = await _handler.Handle(new CerrarSesionCommand(), CancellationToken.None);

        Assert.False(cerrada);
        Assert.False(_sesion.Cerrada);   // no toca la sesión de otra persona
        Assert.Equal(0, _unitOfWork.VecesGuardado);
        Assert.True(_cookie.Eliminada);
    }
}
