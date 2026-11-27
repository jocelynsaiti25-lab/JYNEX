using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

[ApiController]
[Route("api/salidas-medicamento")]
public sealed class SalidaMedicamentosController(ISender sender) : ControllerBase
{
    [HttpGet("medicamentos")]
    public async Task<ActionResult<IReadOnlyList<MedicamentoParaSalidaResponse>>> Buscar(
        [FromQuery] string? termino,
        CancellationToken cancellationToken)
    {
        var resultados = await sender.Send(new BuscarMedicamentosParaSalidaQuery(termino), cancellationToken);

        var response = resultados
            .Select(item => new MedicamentoParaSalidaResponse(
                item.IdMedicamento,
                item.NombreComercial,
                item.NombreSustancia,
                item.Presentacion,
                item.Concentracion,
                item.EsControlado,
                item.ExistenciasVigentes,
                item.EstaAgotado,
                item.Lotes
                    .Select(lote => new LoteVigenteResponse(
                        lote.IdLote,
                        lote.NumeroLote,
                        lote.FechaCaducidad.ToString("yyyy-MM-dd"),
                        lote.Existencias,
                        lote.Semaforo))
                    .ToList()))
            .ToList();

        return Ok(response);
    }

    [HttpGet("propuesta")]
    public async Task<ActionResult<PropuestaSalidaResponse>> Proponer(
        [FromQuery] int? idMedicamento,
        [FromQuery] string? cantidad,
        CancellationToken cancellationToken)
    {
        var propuesta = await sender.Send(
            new ProponerSalidaMedicamentoQuery(idMedicamento ?? 0, cantidad ?? string.Empty), cancellationToken);

        return Ok(new PropuestaSalidaResponse(
            propuesta.IdMedicamento,
            propuesta.Medicamento,
            propuesta.CantidadTotal,
            propuesta.ExistenciasVigentes,
            AResponse(propuesta.Lotes)));
    }

    [HttpPost]
    public async Task<ActionResult<RegistrarSalidaMedicamentoResponse>> Registrar(
        [FromBody] RegistrarSalidaMedicamentoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await sender.Send(
            new RegistrarSalidaMedicamentoCommand(request.IdMedicamento ?? 0, request.Cantidad ?? string.Empty),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new RegistrarSalidaMedicamentoResponse(
            resultado.IdMovimiento,
            resultado.IdMedicamento,
            resultado.Medicamento,
            resultado.CantidadTotal,
            AResponse(resultado.Lotes),
            resultado.Mensaje));
    }

    private static List<LotePropuestoResponse> AResponse(IReadOnlyList<LotePropuestoDto> lotes) =>
        lotes
            .Select(lote => new LotePropuestoResponse(
                lote.IdLote,
                lote.NumeroLote,
                lote.FechaCaducidad.ToString("yyyy-MM-dd"),
                lote.Cantidad))
            .ToList();
}
