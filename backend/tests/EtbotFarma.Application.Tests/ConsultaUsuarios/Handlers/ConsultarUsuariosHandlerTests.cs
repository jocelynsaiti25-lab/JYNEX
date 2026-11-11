using EtbotFarma.Application.ConsultaUsuarios.Dtos;
using EtbotFarma.Application.ConsultaUsuarios.Handlers;
using EtbotFarma.Application.ConsultaUsuarios.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.ConsultaUsuarios.Handlers;

/// <summary>Pruebas de HU-04: consulta y búsqueda de usuarios registrados.</summary>
public class ConsultarUsuariosHandlerTests
{
    private static readonly DateTime Fecha = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly UsuarioRepositoryFalso _usuarios = new();
    private readonly ConsultarUsuariosHandler _handler;

    public ConsultarUsuariosHandlerTests()
    {
        var administrador = Rol.Reconstruir(Rol.Administrador, "Administrador");
        var empleado = Rol.Reconstruir(Rol.Empleado, "Empleado");

        _usuarios.Usuarios.Add(Usuario.Crear("María", "Fernández López", "mflopez", "maria.fl@etbotfarma.com", "hash-secreto-1", "5511111111", empleado, Fecha));
        _usuarios.Usuarios.Add(Usuario.Crear("Ana", "Ruiz", "aruiz", "ana@etbotfarma.com", "hash-secreto-2", "5522222222", administrador, Fecha));
        // Escenario 9: otra persona con el MISMO nombre completo que María
        _usuarios.Usuarios.Add(Usuario.Crear("María", "Fernández López", "mfernandez", "maria.f@etbotfarma.com", "hash-secreto-3", "5533333333", empleado, Fecha));

        _handler = new ConsultarUsuariosHandler(_usuarios);
    }

    private Task<IReadOnlyList<UsuarioRegistradoDto>> ConsultarAsync(string? busqueda)
        => _handler.Handle(new ConsultarUsuariosQuery(busqueda), CancellationToken.None);

    /// <summary>Escenario 1: lista con nombre completo, usuario, correo, teléfono y rol.</summary>
    [Fact]
    public async Task Sin_busqueda_devuelve_todas_las_cuentas_con_sus_datos_y_rol()
    {
        var lista = await ConsultarAsync(null);

        Assert.Equal(3, lista.Count);
        var maria = Assert.Single(lista, u => u.NombreUsuario == "mflopez");
        Assert.Equal("María Fernández López", maria.NombreCompleto);
        Assert.Equal("maria.fl@etbotfarma.com", maria.Correo);
        Assert.Equal("5511111111", maria.Telefono);
        Assert.Equal("Empleado", maria.Rol);
        Assert.Equal("Administrador", Assert.Single(lista, u => u.NombreUsuario == "aruiz").Rol);
    }

    /// <summary>Escenario 4: coincidencia parcial en el nombre de usuario.</summary>
    [Fact]
    public async Task La_busqueda_parcial_devuelve_solo_las_cuentas_que_coinciden()
    {
        var lista = await ConsultarAsync("lopez");

        Assert.Equal("mflopez", Assert.Single(lista).NombreUsuario);
    }

    /// <summary>Escenario 10: se ignoran espacios sobrantes y mayúsculas.</summary>
    [Fact]
    public async Task La_busqueda_ignora_espacios_y_mayusculas()
    {
        var lista = await ConsultarAsync("  MFLopez  ");

        Assert.Equal("mflopez", Assert.Single(lista).NombreUsuario);
        Assert.Equal("MFLopez", _usuarios.UltimaBusqueda);   // al repositorio llega sin espacios
    }

    /// <summary>Escenario 5: sin coincidencias la lista viene vacía (la pantalla muestra el mensaje).</summary>
    [Fact]
    public async Task Sin_coincidencias_devuelve_una_lista_vacia()
    {
        var lista = await ConsultarAsync("noexiste");

        Assert.Empty(lista);
    }

    /// <summary>Escenario 6: si se borra el texto (solo espacios o vacío), vuelve la lista completa.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Con_busqueda_vacia_devuelve_la_lista_completa(string busqueda)
    {
        var lista = await ConsultarAsync(busqueda);

        Assert.Equal(3, lista.Count);
        Assert.Null(_usuarios.UltimaBusqueda);
    }

    /// <summary>Escenario 9: dos personas con el mismo nombre se distinguen por usuario y correo.</summary>
    [Fact]
    public async Task Dos_personas_con_el_mismo_nombre_aparecen_como_cuentas_distintas()
    {
        var lista = await ConsultarAsync(null);

        var marias = lista.Where(u => u.NombreCompleto == "María Fernández López").ToList();
        Assert.Equal(2, marias.Count);
        Assert.NotEqual(marias[0].NombreUsuario, marias[1].NombreUsuario);
        Assert.NotEqual(marias[0].Correo, marias[1].Correo);
    }

    /// <summary>Escenario 7: el Dto no tiene NINGÚN campo de contraseña ni de hash.</summary>
    [Fact]
    public void El_dto_no_tiene_contrasenia_ni_hash()
    {
        var propiedades = typeof(UsuarioRegistradoDto).GetProperties().Select(p => p.Name.ToLowerInvariant());

        Assert.DoesNotContain(propiedades, nombre => nombre.Contains("contrasenia") || nombre.Contains("hash") || nombre.Contains("password"));
    }

    /// <summary>Escenarios 7 y 14: los valores del hash no salen y la consulta no agrega ni cambia cuentas.</summary>
    [Fact]
    public async Task La_consulta_no_devuelve_hashes_ni_modifica_cuentas()
    {
        var lista = await ConsultarAsync(null);

        Assert.DoesNotContain(lista, u => u.ToString().Contains("hash-secreto"));
        Assert.Equal(3, _usuarios.Usuarios.Count);
        Assert.Empty(_usuarios.Agregados);
    }

    /// <summary>
    /// Escenario 14: es solo de lectura. El handler ni siquiera recibe IUnitOfWork,
    /// así que es IMPOSIBLE que confirme (COMMIT) un cambio en la base de datos.
    /// </summary>
    [Fact]
    public void El_handler_no_puede_guardar_cambios()
    {
        var dependencias = typeof(ConsultarUsuariosHandler).GetConstructors().Single()
            .GetParameters().Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(EtbotFarma.Application.Common.Interfaces.IUnitOfWork), dependencias);
    }
}
