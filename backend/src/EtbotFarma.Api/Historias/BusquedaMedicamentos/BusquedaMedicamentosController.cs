using EtbotFarma.Application.BusquedaMedicamentos.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.BusquedaMedicamentos;

/// <summary>
/// HU-08: Búsqueda y consulta de medicamentos en el inventario.
/// Protegido por defecto (requiere sesión activa para Administrador o Empleado; HU-08 escenario 8).
/// </summary>
[ApiController]
[Route("api/inventario")]
public sealed class BusquedaMedicamentosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// GET api/inventario?termino=...&semaforo=...
    /// Devuelve los medicamentos y lotes filtrados por término y semáforo de caducidad.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemInventarioResponse>>> Consultar(
        [FromQuery] string? termino,
        [FromQuery] string? semaforo,
        CancellationToken cancellationToken)
    {
        var resultados = await sender.Send(new ConsultarInventarioQuery(termino, semaforo), cancellationToken);

        var response = resultados
            .Select(item => new ItemInventarioResponse(
                item.IdMedicamento,
                item.IdLote,
                item.NombreComercial,
                item.NombreSustancia,
                item.Presentacion,
                item.Concentracion,
                item.EsControlado,
                item.StockMinimo,
                item.NumeroLote,
                item.Existencias,
                item.FechaCaducidad?.ToString("yyyy-MM-dd"),
                item.SemaforoCaducidad,
                item.EstadoDisponibilidad))
            .ToList();

        return Ok(response);
    }
}