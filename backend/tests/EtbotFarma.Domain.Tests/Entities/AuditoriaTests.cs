using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

public class AuditoriaTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Se_crea_con_quien_que_sobre_que_registro_y_cuando()
    {
        var auditoria = Auditoria.Crear(1, Auditoria.CrearUsuario, Auditoria.TablaUsuario, 7, null, "{}", Ahora);

        Assert.Equal(1, auditoria.IdUsuario);
        Assert.Equal("CREAR USUARIO", auditoria.Accion);
        Assert.Equal("usuario", auditoria.TablaAfectada);
        Assert.Equal(7, auditoria.IdRegistro);
        Assert.Null(auditoria.ValorAnterior);
        Assert.Equal(Ahora, auditoria.Fecha);
    }

    [Theory]
    [InlineData(0, "CREAR USUARIO", "usuario", 7)]   // sin quién lo hizo
    [InlineData(1, "", "usuario", 7)]                // sin acción
    [InlineData(1, "CREAR USUARIO", "", 7)]          // sin tabla
    [InlineData(1, "CREAR USUARIO", "usuario", 0)]   // sin registro afectado
    public void Sin_datos_obligatorios_no_se_crea(int idUsuario, string accion, string tabla, int idRegistro)
    {
        Assert.Throws<DomainException>(() => Auditoria.Crear(idUsuario, accion, tabla, idRegistro, null, null, Ahora));
    }

    [Fact]
    public void Accion_de_mas_de_50_caracteres_no_se_crea()
    {
        Assert.Throws<DomainException>(
            () => Auditoria.Crear(1, new string('A', 51), Auditoria.TablaUsuario, 7, null, null, Ahora));
    }
}
