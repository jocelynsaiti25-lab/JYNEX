using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.CreacionUsuarios.Commands;
using EtbotFarma.Application.CreacionUsuarios.Handlers;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.CreacionUsuarios.Handlers;

/// <summary>Pruebas de HU-03: creación de usuarios.</summary>
public class CrearUsuarioHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly UsuarioRepositoryFalso _usuarios = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly AuditoriaRepositoryFalso _auditorias = new();
    private readonly UsuarioActualFalso _administrador = new() { IdUsuario = 1, NombreUsuario = "admin", Rol = "Administrador" };
    private readonly CrearUsuarioHandler _handler;

    private static readonly CrearUsuarioCommand Nuevo = new(
        "María Fernanda", "López García", "mflopez", "Maria.Lopez@Gmail.com", "5512345678", "Farmacia2026", Rol.Empleado);

    public CrearUsuarioHandlerTests()
    {
        _handler = new CrearUsuarioHandler(
            _usuarios, new RolRepositoryFalso(), _auditorias, _administrador, new PasswordHasherFalso(), _unitOfWork, new RelojFalso(Ahora));

        // Ya existe una cuenta: Juan Pérez Gómez (usuario jperez, correo juan.perez@gmail.com)
        _usuarios.Usuarios.Add(Usuario.Crear(
            "Juan", "Pérez Gómez", "jperez", "juan.perez@gmail.com", "hash:Otra12345", "5599999999",
            Rol.Reconstruir(Rol.Empleado, "Empleado"), Ahora.UtcDateTime));
    }

    [Fact]
    public async Task Con_datos_correctos_crea_la_cuenta_activa_con_contrasenia_encriptada()
    {
        var resultado = await _handler.Handle(Nuevo, CancellationToken.None);

        var usuario = Assert.Single(_usuarios.Agregados);
        Assert.True(usuario.Activo);
        Assert.Equal("hash:Farmacia2026", usuario.ContraseniaHash);   // nunca en texto plano (criterio 5)
        Assert.Equal("maria.lopez@gmail.com", usuario.Correo);         // correo en minúsculas
        Assert.Equal(Rol.Empleado, usuario.IdRol);
        Assert.Equal(1, _unitOfWork.VecesGuardado);

        Assert.Equal("María Fernanda López García", resultado.NombreCompleto);
        Assert.Equal("Empleado", resultado.Rol);
        Assert.True(resultado.IdUsuario > 0);
    }

    [Fact]
    public async Task Con_correo_ya_registrado_no_crea_la_cuenta()
    {
        // Mismo correo aunque cambien mayúsculas: es la misma persona (escenario 7)
        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(
            Nuevo with { NombreUsuario = "otro.usuario", Correo = "JUAN.PEREZ@gmail.com" }, CancellationToken.None));

        Assert.Equal("Este correo ya está registrado.", ex.Message);
        Assert.Empty(_usuarios.Agregados);
        Assert.Empty(_auditorias.Registros);   // sin usuario creado tampoco hay auditoría
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Con_nombre_de_usuario_en_uso_no_crea_la_cuenta()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(
            Nuevo with { NombreUsuario = "jperez" }, CancellationToken.None));

        Assert.Equal("El nombre de usuario ya está en uso, elige otro.", ex.Message);
        Assert.Empty(_usuarios.Agregados);
    }

    /// <summary>Escenario 9: dos personas pueden llamarse igual y compartir teléfono; las distingue su correo.</summary>
    [Fact]
    public async Task Persona_con_el_mismo_nombre_y_telefono_pero_otro_correo_si_se_crea()
    {
        var homonimo = new CrearUsuarioCommand(
            "Juan", "Pérez Gómez", "jperez2", "juanpg@hotmail.com", "5599999999", "Farmacia2026", Rol.Empleado);

        var resultado = await _handler.Handle(homonimo, CancellationToken.None);

        Assert.Equal("Juan Pérez Gómez", resultado.NombreCompleto);
        Assert.Single(_usuarios.Agregados);
    }

    [Fact]
    public async Task Con_rol_que_no_existe_no_crea_la_cuenta()
    {
        await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(Nuevo with { IdRol = 99 }, CancellationToken.None));

        Assert.Empty(_usuarios.Agregados);
    }

    /// <summary>Corrección de HU-03: cada usuario creado deja su registro de auditoría en el MISMO guardado.</summary>
    [Fact]
    public async Task Al_crear_el_usuario_registra_la_auditoria_con_el_administrador_y_sin_contrasenia()
    {
        var resultado = await _handler.Handle(Nuevo, CancellationToken.None);

        var auditoria = Assert.Single(_auditorias.Registros);
        Assert.Equal(1, auditoria.IdUsuario);                    // quién: el administrador con sesión
        Assert.Equal("CREAR USUARIO", auditoria.Accion);          // qué hizo
        Assert.Equal("usuario", auditoria.TablaAfectada);
        Assert.Equal(resultado.IdUsuario, auditoria.IdRegistro);  // qué usuario se creó
        Assert.Equal(Ahora.UtcDateTime, auditoria.Fecha);         // cuándo (UTC)
        Assert.Null(auditoria.ValorAnterior);
        Assert.Contains("mflopez", auditoria.ValorNuevo);
        Assert.DoesNotContain("Farmacia2026", auditoria.ValorNuevo!);   // nunca la contraseña
        Assert.DoesNotContain("hash:", auditoria.ValorNuevo!);          // ni su hash
        Assert.Equal(1, _unitOfWork.VecesGuardado);                     // usuario + auditoría: un solo COMMIT
    }

    [Fact]
    public async Task Sin_usuario_con_sesion_no_crea_nada()
    {
        _administrador.IdUsuario = null;

        await Assert.ThrowsAsync<NoAutenticadoException>(() => _handler.Handle(Nuevo, CancellationToken.None));

        Assert.Empty(_usuarios.Agregados);
        Assert.Empty(_auditorias.Registros);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }
}
