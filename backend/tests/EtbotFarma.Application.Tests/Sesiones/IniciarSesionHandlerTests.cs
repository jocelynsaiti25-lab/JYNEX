using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Features.Sesiones.HU01_IniciarSesion;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Sesiones;

/// <summary>Pruebas de HU-01: iniciar sesión.</summary>
public class IniciarSesionHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly UsuarioRepositoryFalso _usuarios = new();
    private readonly SesionRepositoryFalso _sesiones = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly IniciarSesionHandler _handler;

    public IniciarSesionHandlerTests()
    {
        _handler = new IniciarSesionHandler(
            _usuarios, _sesiones, new PasswordHasherFalso(), new TokenSesionServiceFalso(), _unitOfWork, new RelojFalso(Ahora));

        _usuarios.Usuarios.Add(Usuario.Crear(
            "Ana", "López", "ana", "hash:Secreta123", "5512345678",
            new Rol(Rol.Administrador, "Administrador"), Ahora.UtcDateTime));
    }

    [Fact]
    public async Task Con_credenciales_correctas_crea_la_sesion()
    {
        var resultado = await _handler.Handle(new IniciarSesionCommand("ana", "Secreta123"), CancellationToken.None);

        Assert.Equal("token-123", resultado.Token);
        Assert.Equal("Ana López", resultado.NombreCompleto);
        Assert.Equal("Administrador", resultado.Rol);

        var sesion = Assert.Single(_sesiones.Sesiones);
        Assert.Equal("h:token-123", sesion.Token);   // se guarda el HASH, no el token
        Assert.Equal(1, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Con_contrasenia_incorrecta_lanza_error_generico()
    {
        var ex = await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _handler.Handle(new IniciarSesionCommand("ana", "Equivocada"), CancellationToken.None));

        Assert.Equal("Usuario o contraseña incorrectos.", ex.Message);
        Assert.Empty(_sesiones.Sesiones);
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

    [Fact]
    public async Task Sin_datos_lanza_errores_de_validacion()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(
            () => _handler.Handle(new IniciarSesionCommand("", ""), CancellationToken.None));

        Assert.Contains("El nombre de usuario es obligatorio.", ex.Errores);
        Assert.Contains("La contraseña es obligatoria.", ex.Errores);
    }
}
