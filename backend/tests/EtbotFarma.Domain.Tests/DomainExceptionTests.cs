using EtbotFarma.Domain.Common;

namespace EtbotFarma.Domain.Tests;

/// <summary>
/// Aquí irán las pruebas de las reglas de negocio:
/// semáforo de caducidad, FEFO, medicamento agotado, etc.
/// </summary>
public class DomainExceptionTests
{
    [Fact]
    public void Conserva_el_mensaje_de_la_regla_de_negocio()
    {
        var ex = new DomainException("Existencias insuficientes");

        Assert.Equal("Existencias insuficientes", ex.Message);
    }
}
