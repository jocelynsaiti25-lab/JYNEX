using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Handlers;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.Tests.Falsos;

namespace EtbotFarma.Application.Tests.BitacoraMovimientos.Handlers;

public class ConsultarMedicamentosBitacoraHandlerTests
{
    [Fact]
    public async Task Devuelve_las_opciones_de_medicamentos_con_historial_incluidos_inactivos()
    {
        var opciones = new[]
        {
            new MedicamentoBitacoraDto(7, "Nombre actual", "Sustancia")
        };
        var repositorio = new MovimientoRepositoryFalso { MedicamentosBitacora = opciones };
        var handler = new ConsultarMedicamentosBitacoraHandler(repositorio);

        var resultado = await handler.Handle(new ConsultarMedicamentosBitacoraQuery(), CancellationToken.None);

        Assert.Same(opciones, resultado);
    }
}
