using EtbotFarma.Application.EdicionMedicamentos.Commands;
using EtbotFarma.Application.EdicionMedicamentos.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.EdicionMedicamentos;

/// <summary>
/// HU-09: edición de información descriptiva de medicamentos. Comparte el prefijo "api/medicamentos"
/// con HU-06 porque es el mismo tema.
///
/// HU-09 escenarios 3 y 4: el SERVIDOR revisa que sea Administrador ([Authorize(Roles = "Administrador")]):
/// Empleado → 403, sin sesión → 401, y no se modifica ningún dato.
/// </summary>
[ApiController]
[Route("api/medicamentos")]
[Authorize(Roles = "Administrador")]
public sealed class EdicionMedicamentosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// GET api/medicamentos/{idMedicamento} → datos para precargar el formulario (escenario 2)
    ///   200 → datos · 404 → "El medicamento no existe" · 401/403 → sin permisos
    /// </summary>
    [HttpGet("{idMedicamento:int}")]
    public async Task<ActionResult<MedicamentoParaEditarResponse>> ObtenerParaEditar(
        int idMedicamento, CancellationToken cancellationToken)
    {
        var medicamento = await sender.Send(new ObtenerMedicamentoParaEditarQuery(idMedicamento), cancellationToken);

        return Ok(new MedicamentoParaEditarResponse(
            medicamento.IdMedicamento,
            medicamento.NombreComercial,
            medicamento.NombreSustancia,
            medicamento.Presentacion,
            medicamento.Concentracion,
            medicamento.EsControlado,
            medicamento.ExistenciasTotales,
            medicamento.SemaforoCaducidad));
    }

    /// <summary>
    /// PUT api/medicamentos/{idMedicamento}
    ///   200 → actualizado (o sin cambios, escenario 12) y auditoría registrada
    ///   400 → campos obligatorios vacíos (validator) · 404 → "El medicamento no existe"
    ///   422 → duplicado en el catálogo · 401/403 → sin permisos
    /// </summary>
    [HttpPut("{idMedicamento:int}")]
    public async Task<ActionResult<EditarMedicamentoResponse>> Editar(
        int idMedicamento,
        [FromBody] EditarMedicamentoRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Request → Command
        var command = new EditarMedicamentoCommand(
            idMedicamento,
            request.NombreComercial ?? string.Empty,
            request.NombreSustancia ?? string.Empty,
            request.Presentacion ?? string.Empty,
            request.Concentracion ?? string.Empty,
            request.EsControlado);

        // 2. MediatR: ValidationBehavior → EditarMedicamentoHandler
        var resultado = await sender.Send(command, cancellationToken);

        // 3. Dto → Response
        return Ok(new EditarMedicamentoResponse(
            resultado.IdMedicamento,
            resultado.NombreComercial,
            resultado.NombreSustancia,
            resultado.Presentacion,
            resultado.Concentracion,
            resultado.EsControlado,
            resultado.SeGuardaronCambios,
            resultado.Mensaje));
    }
}
