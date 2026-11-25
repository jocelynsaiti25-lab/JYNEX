using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.ReasignacionContrasena.Dtos;
using EtbotFarma.Application.ReasignacionContrasena.Handlers;
using EtbotFarma.Application.ReasignacionContrasena.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.ReasignacionContrasena.Handlers;

/// <summary>Pruebas de HU-05: datos de la cuenta para el formulario de reasignación.</summary>
public class ObtenerCuentaHandlerTests
{
    private readonly UsuarioRepositoryFalso _usuarios = new();
    private readonly ObtenerCuentaHandler _handler;

    public ObtenerCuentaHandlerTests()
    {
        _usuarios.Usuarios.Add(Usuario.Reconstruir(7, "Ana", "López", "ana", "ana@etbotfarma.com", "hash-secreto", "5512345678",
            Rol.Reconstruir(Rol.Empleado, "Empleado"), activo: false, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        _handler = new ObtenerCuentaHandler(_usuarios);
    }

    /// <summary>Escenario 2: datos para la confirmación (nombre, usuario, correo) y si está activa (escenario 8).</summary>
    [Fact]
    public async Task Devuelve_los_datos_de_la_cuenta()
    {
        var cuenta = await _handler.Handle(new ObtenerCuentaQuery(7), CancellationToken.None);

        Assert.Equal("Ana López", cuenta.NombreCompleto);
        Assert.Equal("ana", cuenta.NombreUsuario);
        Assert.Equal("ana@etbotfarma.com", cuenta.Correo);
        Assert.Equal("Empleado", cuenta.Rol);
        Assert.False(cuenta.Activo);
    }

    /// <summary>Escenario 7: una cuenta escrita a mano que no existe → "La cuenta no existe".</summary>
    [Fact]
    public async Task Con_una_cuenta_que_no_existe_lanza_NoEncontradoException()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => _handler.Handle(new ObtenerCuentaQuery(999), CancellationToken.None));

        Assert.Equal("La cuenta no existe", ex.Message);
    }

    /// <summary>Escenario 4: el Dto nunca trae la contraseña anterior ni su hash.</summary>
    [Fact]
    public void El_dto_no_tiene_contrasenia_ni_hash()
    {
        var propiedades = typeof(CuentaDto).GetProperties().Select(p => p.Name.ToLowerInvariant());

        Assert.DoesNotContain(propiedades, nombre => nombre.Contains("contrasenia") || nombre.Contains("hash"));
    }
}
