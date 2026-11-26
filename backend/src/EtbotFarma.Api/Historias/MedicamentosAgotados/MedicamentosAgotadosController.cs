using EtbotFarma.Application.MedicamentosAgotados.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.MedicamentosAgotados;

/// <summary>
/// HU-12: consulta de medicamentos agotados. Comparte el prefijo "api/medicamentos" porque es
/// el mismo tema (igual que HU-06, HU-09 y HU-11). Disponible para cualquier usuario autenticado.
/// </summary>
[ApiController]
[Route("api/medicamentos")]
public sealed class MedicamentosAgotadosController(ISender sender) : ControllerBase
{
    /// <summary>GET api/medicamentos/agotados (escenarios 1 al 3): lista sin existencias vigentes.</summary>
    [HttpGet("agotados")]
    public async Task<ActionResult<IReadOnlyList<MedicamentoAgotadoResponse>>> ObtenerAgotados(
        CancellationToken cancellationToken)
    {
        var resultados = await sender.Send(new ObtenerMedicamentosAgotadosQuery(), cancellationToken);

        var response = resultados
            .Select(item => new MedicamentoAgotadoResponse(
                item.IdMedicamento,
                item.NombreComercial,
                item.NombreSustancia,
                item.Presentacion,
                item.Concentracion))
            .ToList();

        return Ok(response);
    }
}
