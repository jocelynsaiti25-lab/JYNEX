using EtbotFarma.Application.InicioSesion.Commands;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EtbotFarma.Application.Tests.Common;

/// <summary>
/// Comprueba el pipeline REAL de MediatR (el mismo que usa la API):
/// los validadores se ejecutan solos ANTES del handler.
/// </summary>
public class ValidationBehaviorTests
{
    private readonly SesionRepositoryFalso _sesiones = new();
    private readonly ServiceProvider _proveedor;
    private readonly ISender _sender;

    public ValidationBehaviorTests()
    {
        var usuarios = new UsuarioRepositoryFalso();
        usuarios.Usuarios.Add(Usuario.Crear("Ana", "López", "ana", "hash:Secreta123", "5512345678",
            Rol.Reconstruir(Rol.Empleado, "Empleado"), DateTime.UtcNow));

        var services = new ServiceCollection();
        services.AddApplication();   // mismo registro que Program.cs
        services.AddSingleton<IUsuarioRepository>(usuarios);
        services.AddSingleton<ISesionRepository>(_sesiones);
        services.AddSingleton<IPasswordHasher, PasswordHasherFalso>();
        services.AddSingleton<ITokenSesionService, TokenSesionServiceFalso>();
        services.AddSingleton<IUnitOfWork, UnitOfWorkFalso>();
        services.AddSingleton<IDateTimeProvider>(new RelojFalso(DateTimeOffset.UtcNow));
        services.AddSingleton<ICookieSesionService>(new CookieSesionServiceFalso());

        services.AddSingleton(new ConfiguracionSesion());

        _proveedor = services.BuildServiceProvider();
        _sender = _proveedor.GetRequiredService<ISender>();
    }

    [Fact]
    public async Task Con_datos_invalidos_MediatR_lanza_ValidacionException_y_no_ejecuta_el_handler()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(
            () => _sender.Send(new IniciarSesionCommand("", "")));

        Assert.Contains("El nombre de usuario es obligatorio.", ex.Errores);
        Assert.Contains("La contraseña es obligatoria.", ex.Errores);
        Assert.Empty(_sesiones.Sesiones);   // el handler nunca se ejecutó
    }

    [Fact]
    public void Los_validators_se_registran_solos_en_la_inyeccion_de_dependencias()
    {
        var validators = _proveedor.GetServices<IValidator<IniciarSesionCommand>>();

        Assert.Single(validators);
    }

    [Fact]
    public async Task Un_command_sin_validator_llega_directo_al_handler()
    {
        var resultado = await _sender.Send(new ValidarSesionCommand("token-que-no-existe"));

        Assert.Null(resultado);   // lo respondió ValidarSesionHandler, sin error de validación
    }

    [Fact]
    public async Task Con_datos_validos_MediatR_ejecuta_el_handler()
    {
        var resultado = await _sender.Send(new IniciarSesionCommand("ana", "Secreta123"));

        Assert.Equal("ana", resultado.NombreUsuario);
        Assert.Single(_sesiones.Sesiones);
    }
}
