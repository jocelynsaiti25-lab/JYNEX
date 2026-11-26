using EtbotFarma.Application.MedicamentosAgotados.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.MedicamentosAgotados;

[ApiController]
[Route("api/inventario/agotados")]
public sealed class MedicamentosAgotadosController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MedicamentoAgotadoResponse>>> Consultar(
        [FromQuery] string? termino,
        CancellationToken cancellationToken)
    {
        var resultados = await sender.Send(new ObtenerMedicamentosAgotadosQuery(termino), cancellationToken);

        var response = resultados
            .Select(item => new MedicamentoAgotadoResponse(
                item.IdMedicamento,
                item.NombreComercial,
                item.NombreSustancia,
                item.Presentacion,
                item.Concentracion,
                item.Etiqueta))
            .ToList();

        return Ok(response);
    }
}
