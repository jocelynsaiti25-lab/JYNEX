using EtbotFarma.Application.StockMinimo.Commands;
using EtbotFarma.Application.StockMinimo.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.StockMinimo;

/// <summary>
/// HU-11: control de stock mínimo del catálogo de medicamentos.
/// Comparte el prefijo "api/medicamentos" porque es el mismo tema (igual que HU-06).
/// Disponible para cualquier usuario autenticado (Administrador o Empleado): sin [Authorize(Roles)].
/// </summary>
[ApiController]
[Route("api/medicamentos")]
public sealed class StockMinimoController(ISender sender) : ControllerBase
{
    /// <summary>GET api/medicamentos/stock-minimo (escenario 1): lista para la pantalla de control.</summary>
    [HttpGet("stock-minimo")]
    public async Task<ActionResult<IReadOnlyList<MedicamentoStockResponse>>> ObtenerParaControl(
        CancellationToken cancellationToken)
    {
        var resultados = await sender.Send(new ObtenerMedicamentosParaStockMinimoQuery(), cancellationToken);

        var response = resultados
            .Select(item => new MedicamentoStockResponse(
                item.IdMedicamento,
                item.NombreComercial,
                item.NombreSustancia,
                item.Presentacion,
                item.Concentracion,
                item.StockMinimo,
                item.ExistenciasVigentes,
                item.ExistenciasCaducadas,
                item.TieneLotesCaducados,
                item.EnStockBajo,
                item.EstaAgotado))
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// PUT api/medicamentos/{idMedicamento}/stock-minimo (escenarios 1 y 6)
    ///   200 → actualizado · 400 → valor inválido (validator) · 404 → "El medicamento no existe"
    /// </summary>
    [HttpPut("{idMedicamento:int}/stock-minimo")]
    public async Task<ActionResult<ActualizarStockMinimoResponse>> Actualizar(
        int idMedicamento,
        [FromBody] ActualizarStockMinimoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await sender.Send(
            new ActualizarStockMinimoCommand(idMedicamento, request.NuevoStockMinimo), cancellationToken);

        return Ok(new ActualizarStockMinimoResponse(resultado.IdMedicamento, resultado.StockMinimo, resultado.Mensaje));
    }
}
