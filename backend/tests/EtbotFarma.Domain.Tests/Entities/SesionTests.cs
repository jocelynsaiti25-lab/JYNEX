using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

/// <summary>Reglas de la sesión (HU-01 criterio 6: expira por inactividad).</summary>
public class SesionTests
{
    private static readonly DateTime Inicio = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Veinte = TimeSpan.FromMinutes(20);

    private static Usuario CrearUsuario()
        => Usuario.Crear("Ana", "López", "ana", "ana@etbotfarma.com", "hash", "5512345678", Rol.Reconstruir(Rol.Empleado, "Empleado"), Inicio);

    [Fact]
    public void Sesion_nueva_esta_vigente()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio, Veinte);

        Assert.True(sesion.EstaVigente(Inicio.AddMinutes(19)));
    }

    [Fact]
    public void Sesion_expira_despues_del_tiempo_de_inactividad()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio, Veinte);

        Assert.False(sesion.EstaVigente(Inicio.AddMinutes(21)));
    }

    [Fact]
    public void Registrar_actividad_reinicia_el_contador()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio, Veinte);

        sesion.RegistrarActividad(Inicio.AddMinutes(15), Veinte);

        Assert.True(sesion.EstaVigente(Inicio.AddMinutes(30)));
    }

    [Fact]
    public void Sesion_cerrada_no_esta_vigente()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio, Veinte);

        sesion.Cerrar(Inicio.AddMinutes(1));

        Assert.False(sesion.EstaVigente(Inicio.AddMinutes(2)));
        Assert.True(sesion.Cerrada);
        Assert.Equal(Inicio.AddMinutes(1), sesion.FechaCierre);
    }

    [Fact]
    public void La_fecha_de_expiracion_es_el_inicio_mas_el_tiempo_de_inactividad()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio, Veinte);

        Assert.Equal(Inicio.AddMinutes(20), sesion.FechaExpiracion);
        Assert.False(sesion.Cerrada);
    }

    [Fact]
    public void Registrar_actividad_recorre_la_fecha_de_expiracion()
    {
        var sesion = Sesion.Iniciar(CrearUsuario(), "token-hash", Inicio, Veinte);

        sesion.RegistrarActividad(Inicio.AddMinutes(15), Veinte);

        Assert.Equal(Inicio.AddMinutes(35), sesion.FechaExpiracion);
    }

    [Fact]
    public void Reconstruir_arma_la_sesion_con_los_datos_de_la_base()
    {
        var sesion = Sesion.Reconstruir(5, CrearUsuario(), "token-hash", Inicio, Inicio, Inicio.AddMinutes(20), Inicio.AddMinutes(3));

        Assert.Equal(5, sesion.Id);
        Assert.True(sesion.Cerrada);
        Assert.False(sesion.EstaVigente(Inicio.AddMinutes(4)));
    }

    [Fact]
    public void No_se_puede_iniciar_sesion_con_usuario_inactivo()
    {
        var usuario = CrearUsuario();
        usuario.Desactivar();

        Assert.Throws<DomainException>(() => Sesion.Iniciar(usuario, "token-hash", Inicio, Veinte));
    }
}
