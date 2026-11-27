using System.Reflection;
using EtbotFarma.Api.Historias.BitacoraMovimientos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace EtbotFarma.Api.IntegrationTests.Historias.BitacoraMovimientos;

public class BitacoraMovimientosControllerTests
{
    [Fact]
    public void Controller_restringe_la_consulta_al_rol_Administrador()
    {
        var autorizacion = typeof(BitacoraMovimientosController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(autorizacion);
        Assert.Equal("Administrador", autorizacion.Roles);
    }

    [Fact]
    public void Controller_expone_solo_rutas_GET_de_bitacora()
    {
        var tipo = typeof(BitacoraMovimientosController);
        var ruta = tipo.GetCustomAttribute<RouteAttribute>();
        var acciones = tipo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(metodo => metodo.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();

        Assert.NotNull(ruta);
        Assert.Equal("api/bitacora/movimientos", ruta.Template);
        Assert.Equal(2, acciones.Length);
        Assert.All(acciones, accion =>
        {
            Assert.Contains("GET", accion.GetCustomAttribute<HttpGetAttribute>()?.HttpMethods ?? []);
            Assert.Empty(accion.GetCustomAttributes<HttpPostAttribute>());
            Assert.Empty(accion.GetCustomAttributes<HttpPutAttribute>());
            Assert.Empty(accion.GetCustomAttributes<HttpDeleteAttribute>());
        });
        Assert.Contains(acciones, accion => accion.GetCustomAttribute<HttpGetAttribute>()?.Template == "medicamentos");
    }
}
