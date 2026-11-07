using EtbotFarma.Application.Commands.InicioSesion;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Handlers.InicioSesion;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Handlers.InicioSesion;

/// <summary>Pruebas de HU-01: iniciar sesión.</summary>
public class IniciarSesionHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly UsuarioRepositoryFalso _usuarios = new();
    private readonly SesionRepositoryFalso _sesiones = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly CookieSesionServiceFalso _cookie = new();
    private readonly IniciarSesionHandler _handler;

    public IniciarSesionHandlerTests()
    {
        _handler = new IniciarSesionHandler(
            _usuarios, _sesiones, new PasswordHasherFalso(), new TokenSesionServiceFalso(), _unitOfWork, new RelojFalso(Ahora), _cookie,
            new ConfiguracionSesion { MinutosInactividad = 20 });

        _usuarios.Usuarios.Add(Usuario.Crear(
            "Ana", "López", "ana", "hash:Secreta123", "5512345678",
            new Rol(Rol.Administrador, "Administrador"), Ahora.UtcDateTime));
    }

    [Fact]
    public async Task Con_credenciales_correctas_crea_la_sesion()
    {
        var resultado = await _handler.Handle(new IniciarSesionCommand("ana", "Secreta123"), CancellationToken.None);

        Assert.Equal("token-123", _cookie.TokenGuardado);   // el token va a la cookie, no al JSON
        Assert.Equal("Ana López", resultado.NombreCompleto);
        Assert.Equal("Administrador", resultado.Rol);

        var sesion = Assert.Single(_sesiones.Sesiones);
        Assert.Equal("h:token-123", sesion.TokenHash);   // se guarda el HASH, no el token
        Assert.Equal(Ahora.AddMinutes(20).UtcDateTime, sesion.FechaExpiracion);
        Assert.Equal(1, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Con_contrasenia_incorrecta_lanza_error_generico()
    {
        var ex = await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _handler.Handle(new IniciarSesionCommand("ana", "Equivocada"), CancellationToken.None));

        Assert.Equal("Usuario o contraseña incorrectos.", ex.Message);
        Assert.Empty(_sesiones.Sesiones);
        Assert.Null(_cookie.TokenGuardado);
    }

    [Fact]
    public async Task Con_usuario_que_no_existe_lanza_el_mismo_error_generico()
    {
        var ex = await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _handler.Handle(new IniciarSesionCommand("noexiste", "Secreta123"), CancellationToken.None));

        Assert.Equal("Usuario o contraseña incorrectos.", ex.Message);
    }

    [Fact]
    public async Task Con_cuenta_inactiva_no_inicia_sesion()
    {
        _usuarios.Usuarios[0].Desactivar();

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _handler.Handle(new IniciarSesionCommand("ana", "Secreta123"), CancellationToken.None));

        Assert.Empty(_sesiones.Sesiones);
    }
}
