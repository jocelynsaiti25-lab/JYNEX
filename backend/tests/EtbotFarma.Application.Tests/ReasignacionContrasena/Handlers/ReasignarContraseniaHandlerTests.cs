using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.ReasignacionContrasena.Commands;
using EtbotFarma.Application.ReasignacionContrasena.Handlers;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.ReasignacionContrasena.Handlers;

/// <summary>Pruebas de HU-05: reasignación de contraseña.</summary>
public class ReasignarContraseniaHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private const int IdAdmin = 1;
    private const int IdAna = 7;

    private readonly UsuarioRepositoryFalso _usuarios = new();
    private readonly SesionRepositoryFalso _sesiones = new();
    private readonly AuditoriaRepositoryFalso _auditorias = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly CookieSesionServiceFalso _cookie = new();
    private readonly UsuarioActualFalso _usuarioActual = new() { IdUsuario = IdAdmin, NombreUsuario = "admin", Rol = "Administrador" };
    private readonly Usuario _ana;
    private readonly Usuario _admin;
    private readonly ReasignarContraseniaHandler _handler;

    public ReasignarContraseniaHandlerTests()
    {
        var empleado = Rol.Reconstruir(Rol.Empleado, "Empleado");
        var administrador = Rol.Reconstruir(Rol.Administrador, "Administrador");

        _ana = Usuario.Reconstruir(IdAna, "Ana", "López", "ana", "ana@etbotfarma.com", "hash:Vieja2026", "5512345678",
            empleado, activo: true, Ahora.UtcDateTime);
        _admin = Usuario.Reconstruir(IdAdmin, "Admin", "Sistema", "admin", "admin@etbotfarma.com", "hash:Admin2026", "5500000000",
            administrador, activo: true, Ahora.UtcDateTime);
        _usuarios.Usuarios.AddRange([_ana, _admin]);

        // Ana tiene dos sesiones abiertas (por ejemplo, en dos computadoras) y una ya cerrada
        _sesiones.Sesiones.Add(Sesion.Iniciar(_ana, "h:pc-1", Ahora.UtcDateTime.AddMinutes(-5), TimeSpan.FromMinutes(20)));
        _sesiones.Sesiones.Add(Sesion.Iniciar(_ana, "h:pc-2", Ahora.UtcDateTime.AddMinutes(-3), TimeSpan.FromMinutes(20)));
        var cerrada = Sesion.Iniciar(_ana, "h:vieja", Ahora.UtcDateTime.AddHours(-2), TimeSpan.FromMinutes(20));
        cerrada.Cerrar(Ahora.UtcDateTime.AddHours(-1));
        _sesiones.Sesiones.Add(cerrada);
        _sesiones.Sesiones.Add(Sesion.Iniciar(_admin, "h:admin", Ahora.UtcDateTime.AddMinutes(-1), TimeSpan.FromMinutes(20)));

        _handler = new ReasignarContraseniaHandler(
            _usuarios, _sesiones, _auditorias, _usuarioActual, new PasswordHasherFalso(), _cookie, _unitOfWork, new RelojFalso(Ahora));
    }

    private Task<EtbotFarma.Application.ReasignacionContrasena.Dtos.ContraseniaReasignadaDto> ReasignarAsync(int idUsuario, string nueva = "Nueva2026")
        => _handler.Handle(new ReasignarContraseniaCommand(idUsuario, nueva, nueva), CancellationToken.None);

    /// <summary>Escenario 1: se guarda el HASH de la nueva contraseña y se confirma una sola vez.</summary>
    [Fact]
    public async Task Guarda_el_hash_de_la_nueva_contrasenia()
    {
        var resultado = await ReasignarAsync(IdAna);

        Assert.Equal("hash:Nueva2026", _ana.ContraseniaHash);   // hash, no el texto
        Assert.Same(_ana, Assert.Single(_usuarios.ContraseniasActualizadas));
        Assert.Equal(1, _unitOfWork.VecesGuardado);
        Assert.Equal("ana", resultado.NombreUsuario);
        Assert.False(resultado.EraSuPropiaCuenta);
    }

    /// <summary>Escenarios 1 y 12: se cierran TODAS las sesiones abiertas del usuario, no las de otros.</summary>
    [Fact]
    public async Task Cierra_todas_las_sesiones_abiertas_del_usuario()
    {
        var resultado = await ReasignarAsync(IdAna);

        Assert.Equal(2, resultado.SesionesCerradas);
        Assert.All(_sesiones.Sesiones.Where(s => s.IdUsuario == IdAna), s => Assert.True(s.Cerrada));
        Assert.Equal(Ahora.UtcDateTime.AddHours(-1), _sesiones.Sesiones.Single(s => s.TokenHash == "h:vieja").FechaCierre);   // ya cerrada: conserva su fecha
        Assert.False(_sesiones.Sesiones.Single(s => s.TokenHash == "h:admin").Cerrada);   // la del admin sigue
        Assert.False(_cookie.Eliminada);   // la cookie del admin no se toca
    }

    /// <summary>Escenario 14: auditoría con quién, a qué cuenta y cuándo, SIN contraseña ni hash.</summary>
    [Fact]
    public async Task Registra_la_auditoria_sin_contrasenia_ni_hash()
    {
        await ReasignarAsync(IdAna);

        var registro = Assert.Single(_auditorias.Registros);
        Assert.Equal(IdAdmin, registro.IdUsuario);
        Assert.Equal("REASIGNAR CONTRASEÑA", registro.Accion);
        Assert.Equal("usuario", registro.TablaAfectada);
        Assert.Equal(IdAna, registro.IdRegistro);
        Assert.Equal(Ahora.UtcDateTime, registro.Fecha);
        var texto = $"{registro.ValorAnterior}{registro.ValorNuevo}";
        Assert.DoesNotContain("Nueva2026", texto);
        Assert.DoesNotContain("hash", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Vieja2026", texto);
    }

    /// <summary>Escenario 7: cuenta que no existe → 404 "La cuenta no existe" y nada se guarda.</summary>
    [Fact]
    public async Task Con_una_cuenta_que_no_existe_no_cambia_nada()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(() => ReasignarAsync(999));

        Assert.Equal("La cuenta no existe", ex.Message);
        Assert.Empty(_usuarios.ContraseniasActualizadas);
        Assert.Empty(_auditorias.Registros);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    /// <summary>Escenario 8: cuenta desactivada → 422 "La cuenta está desactivada" y nada se guarda.</summary>
    [Fact]
    public async Task Con_una_cuenta_desactivada_no_cambia_nada()
    {
        _ana.Desactivar();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ReasignarAsync(IdAna));

        Assert.Equal("La cuenta está desactivada", ex.Message);
        Assert.Equal("hash:Vieja2026", _ana.ContraseniaHash);
        Assert.Empty(_usuarios.ContraseniasActualizadas);
        Assert.All(_sesiones.Sesiones.Where(s => s.TokenHash is "h:pc-1" or "h:pc-2"), s => Assert.False(s.Cerrada));
        Assert.Empty(_auditorias.Registros);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    /// <summary>Escenario 17: el Administrador cambia SU contraseña → se cierran todas sus sesiones y su cookie.</summary>
    [Fact]
    public async Task Si_cambia_su_propia_contrasenia_cierra_su_sesion_actual()
    {
        var resultado = await ReasignarAsync(IdAdmin);

        Assert.True(resultado.EraSuPropiaCuenta);
        Assert.True(_sesiones.Sesiones.Single(s => s.TokenHash == "h:admin").Cerrada);
        Assert.True(_cookie.Eliminada);
        Assert.Equal("hash:Nueva2026", _admin.ContraseniaHash);
    }
}
