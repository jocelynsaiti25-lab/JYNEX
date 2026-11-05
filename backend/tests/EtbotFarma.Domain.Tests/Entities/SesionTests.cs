using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

/// <summary>Reglas de la sesión (HU-01 criterio 6: expira por inactividad).</summary>
public class SesionTests
{
    private static readonly DateTime Inicio = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Veinte = TimeSpan.FromMinutes(20);

    private static Usuario CrearUsuario()
        => Usuario.Crear("Ana", "López", "ana", "hash", "5512345678", new Rol(Rol.Empleado, "Empleado"), Inicio);

    [Fact]
    public void Sesion_nueva_esta_vigente()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio);

        Assert.True(sesion.EstaVigente(Inicio.AddMinutes(19), Veinte));
    }

    [Fact]
    public void Sesion_expira_despues_del_tiempo_de_inactividad()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio);

        Assert.False(sesion.EstaVigente(Inicio.AddMinutes(21), Veinte));
    }

    [Fact]
    public void Registrar_actividad_reinicia_el_contador()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio);

        sesion.RegistrarActividad(Inicio.AddMinutes(15));

        Assert.True(sesion.EstaVigente(Inicio.AddMinutes(30), Veinte));
    }

    [Fact]
    public void Sesion_cerrada_no_esta_vigente()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio);

        sesion.Cerrar(Inicio.AddMinutes(1));

        Assert.False(sesion.EstaVigente(Inicio.AddMinutes(2), Veinte));
        Assert.True(sesion.Cerrada);
        Assert.Equal(Inicio.AddMinutes(1), sesion.FechaCierre);
    }

    [Fact]
    public void No_se_puede_iniciar_sesion_con_usuario_inactivo()
    {
        var usuario = CrearUsuario();
        usuario.Desactivar();

        Assert.Throws<DomainException>(() => Sesion.Iniciar(usuario, "token-hash", Inicio));
    }
}
