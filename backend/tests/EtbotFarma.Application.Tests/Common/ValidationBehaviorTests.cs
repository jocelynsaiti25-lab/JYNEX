using EtbotFarma.Application.Commands.InicioSesion;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;
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
    private readonly ISender _sender;

    public ValidationBehaviorTests()
    {
        var usuarios = new UsuarioRepositoryFalso();
        usuarios.Usuarios.Add(Usuario.Crear("Ana", "López", "ana", "hash:Secreta123", "5512345678",
            new Rol(Rol.Empleado, "Empleado"), DateTime.UtcNow));

        var services = new ServiceCollection();
        services.AddApplication();   // mismo registro que Program.cs
        services.AddSingleton<IUsuarioRepository>(usuarios);
        services.AddSingleton<ISesionRepository>(_sesiones);
        services.AddSingleton<IPasswordHasher, PasswordHasherFalso>();
        services.AddSingleton<ITokenSesionService, TokenSesionServiceFalso>();
        services.AddSingleton<IUnitOfWork, UnitOfWorkFalso>();
        services.AddSingleton<IDateTimeProvider>(new RelojFalso(DateTimeOffset.UtcNow));

        _sender = services.BuildServiceProvider().GetRequiredService<ISender>();
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
    public async Task Con_datos_validos_MediatR_ejecuta_el_handler()
    {
        var resultado = await _sender.Send(new IniciarSesionCommand("ana", "Secreta123"));

        Assert.Equal("ana", resultado.NombreUsuario);
        Assert.Single(_sesiones.Sesiones);
    }
}
