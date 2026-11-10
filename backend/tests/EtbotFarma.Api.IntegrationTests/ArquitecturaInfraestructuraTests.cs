using EtbotFarma.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.IntegrationTests;

/// <summary>
/// Reglas de Infrastructure y Api que se revisan sin MySQL:
///   - la base de datos se usa con SQL a mano (MySqlConnector), sin ORM;
///   - cada controller y sus contratos viven en Api/Historias/&lt;Historia&gt;/.
/// Estas pruebas NO necesitan MySQL (no lleva la categoría "Integracion").
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

    [Fact]
    public void Cada_controller_esta_en_la_carpeta_de_su_historia()
    {
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            Assert.EndsWith("Controller", controller.Name);
            Assert.Equal(typeof(object), controller.BaseType!.BaseType);   // solo hereda ControllerBase

            var partes = (controller.Namespace ?? string.Empty).Split('.');
            Assert.True(partes is ["EtbotFarma", "Api", "Historias", _],
                $"{controller.Name} debe estar en Api/Historias/<Historia>/ (está en {controller.Namespace}).");
        }
    }

    [Fact]
    public void Los_contratos_Request_y_Response_estan_junto_al_controller_de_su_historia()
    {
        var contratos = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsNested && (t.Name.EndsWith("Request") || t.Name.EndsWith("Response")));

        foreach (var contrato in contratos)
        {
            var partes = (contrato.Namespace ?? string.Empty).Split('.');
            Assert.True(partes is ["EtbotFarma", "Api", "Historias", _],
                $"{contrato.Name} debe estar en Api/Historias/<Historia>/ (está en {contrato.Namespace}).");
        }
    }
}
