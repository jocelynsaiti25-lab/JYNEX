using EtbotFarma.Application.InicioSesion.Commands;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.InicioSesion.Dtos;
using EtbotFarma.Application.InicioSesion.Handlers;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Application.InicioSesion.Validators;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.InicioSesion.Handlers;

/// <summary>Pruebas de HU-01: validar la sesión en cada petición.</summary>
public class ValidarSesionHandlerTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly SesionRepositoryFalso _sesiones = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly RelojFalso _reloj = new(Inicio);
    private readonly Usuario _usuario;
    private readonly ValidarSesionHandler _handler;

    public ValidarSesionHandlerTests()
    {
        _usuario = Usuario.Crear("Ana", "López", "ana", "ana@etbotfarma.com", "hash", "5512345678",
            Rol.Reconstruir(Rol.Empleado, "Empleado"), Inicio.UtcDateTime);

        _sesiones.Sesiones.Add(Sesion.Iniciar(_usuario, "h:token-123", Inicio.UtcDateTime, TimeSpan.FromMinutes(20)));

        _handler = new ValidarSesionHandler(
            _sesiones, new TokenSesionServiceFalso(), _unitOfWork, _reloj,
            new ConfiguracionSesion { MinutosInactividad = 20 });
    }

    [Fact]
    public async Task Sesion_vigente_devuelve_el_usuario()
    {
        _reloj.UtcNow = Inicio.AddMinutes(10);

        var usuario = await _handler.Handle(new ValidarSesionCommand("token-123"), CancellationToken.None);

        Assert.NotNull(usuario);
        Assert.Equal("ana", usuario.NombreUsuario);
        Assert.Equal("Empleado", usuario.Rol);
        Assert.Equal(Inicio.AddMinutes(10).UtcDateTime, _sesiones.Sesiones[0].UltimaActividad);
        Assert.Equal(Inicio.AddMinutes(30).UtcDateTime, _sesiones.Sesiones[0].FechaExpiracion);
        Assert.Equal(1, _sesiones.VecesActualizada);   // se mandó el UPDATE
    }

    [Fact]
    public async Task Sesion_inactiva_por_mas_de_20_minutos_se_cierra()
    {
        _reloj.UtcNow = Inicio.AddMinutes(21);

        var usuario = await _handler.Handle(new ValidarSesionCommand("token-123"), CancellationToken.None);

        Assert.Null(usuario);
        Assert.True(_sesiones.Sesiones[0].Cerrada);
    }

    [Fact]
    public async Task Token_desconocido_no_es_valido()
    {
        var usuario = await _handler.Handle(new ValidarSesionCommand("otro-token"), CancellationToken.None);

        Assert.Null(usuario);
    }

    [Fact]
    public async Task Cuenta_desactivada_cierra_la_sesion()
    {
        _usuario.Desactivar();

        var usuario = await _handler.Handle(new ValidarSesionCommand("token-123"), CancellationToken.None);

        Assert.Null(usuario);
        Assert.True(_sesiones.Sesiones[0].Cerrada);
    }
}
