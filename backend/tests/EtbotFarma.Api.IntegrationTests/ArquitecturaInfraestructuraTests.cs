using EtbotFarma.Infrastructure.Persistence;

namespace EtbotFarma.Api.IntegrationTests;

/// <summary>
/// Regla del equipo: la base de datos se usa con SQL a mano (MySqlConnector), sin ORM.
/// Esta prueba NO necesita MySQL (no lleva la categoría "Integracion").
/// </summary>
public class ArquitecturaInfraestructuraTests
{
    [Fact]
    public void Infrastructure_no_usa_Entity_Framework_ni_otro_ORM()
    {
        var referencias = typeof(ConexionMySql).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        Assert.DoesNotContain(referencias, nombre => nombre.StartsWith("Microsoft.EntityFrameworkCore"));
        Assert.DoesNotContain(referencias, nombre => nombre.StartsWith("Dapper"));
        Assert.Contains("MySqlConnector", referencias);
    }
}
