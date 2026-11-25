using EtbotFarma.Application.EntradaLote.Commands;
using EtbotFarma.Application.EntradaLote.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.EntradaLote;

/// <summary>
/// HU-07: Registro de entrada de lote.
/// Protegido por defecto (requiere sesión activa para Administrador o Empleado).
/// </summary>
[ApiController]
[Route("api/entradas-lote")]
public sealed class EntradaLoteController(ISender sender) : ControllerBase
{
    /// <summary>
    /// GET api/entradas-lote/medicamentos?termino=...
    /// Busca medicamentos activos por nombre comercial, sustancia activa o lote (HU-07 escenarios 1 y 2).
    /// </summary>
    [HttpGet("medicamentos")]
    public async Task<ActionResult<IReadOnlyList<MedicamentoActivoResponse>>> BuscarMedicamentosActivos(
        [FromQuery] string? termino,
        CancellationToken cancellationToken)
    {
        var resultados = await sender.Send(new BuscarMedicamentosActivosQuery(termino), cancellationToken);

        var response = resultados
            .Select(m => new MedicamentoActivoResponse(
                m.IdMedicamento,
                m.NombreComercial,
                m.NombreSustancia,
                m.Presentacion,
                m.Concentracion,
                m.EsControlado,
                m.StockMinimo,
                m.ExistenciasTotales,
                m.SemaforoGeneral,
                m.EstaAgotado,
                m.EnStockBajo))
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// POST api/entradas-lote
    /// Registra un nuevo lote para un medicamento activo y su entrada en bitácora (HU-07 escenarios 3 al 13).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RegistrarEntradaLoteResponse>> Registrar(
        [FromBody] RegistrarEntradaLoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegistrarEntradaLoteCommand(
            request.IdMedicamento ?? 0,
            request.NumeroLote ?? string.Empty,
            request.FechaCaducidad ?? string.Empty,
            request.Cantidad ?? string.Empty);

        var resultado = await sender.Send(command, cancellationToken);

        var response = new RegistrarEntradaLoteResponse(
            resultado.IdLote,
            resultado.IdMedicamento,
            resultado.NombreMedicamento,
            resultado.NumeroLote,
            resultado.FechaCaducidad.ToString("yyyy-MM-dd"),
            resultado.CantidadIngresada,
            resultado.SemaforoLote,
            resultado.ExistenciasTotalesActualizadas,
            resultado.SemaforoGeneralActualizado,
            resultado.EstaAgotado,
            resultado.EnStockBajo,
            resultado.Mensaje);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}