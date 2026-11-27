using EtbotFarma.Application.RegistroMedicamentos.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EtbotFarma.Api.Historias.RegistroMedicamentos;

/// <summary>
/// HU-06: Registro de medicamentos.
/// Protegido por defecto (requiere sesión activa) y accesible tanto para Administrador como Empleado
/// (HU-06 escenarios 6 y 7).
/// </summary>
[ApiController]
[Route("api/medicamentos")]
public sealed class RegistroMedicamentosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// POST api/medicamentos
    ///   201 → medicamento y lote inicial registrados
    ///   400 → datos de entrada vacíos o con formato inválido (RegistrarMedicamentoValidator)
    ///   401 → sin sesión activa
    ///   422 → regla de negocio incumplida (lote caducado o medicamento duplicado)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RegistrarMedicamentoResponse>> Registrar(
        [FromBody] RegistrarMedicamentoRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Request → Command
        var command = new RegistrarMedicamentoCommand(
            request.NombreComercial ?? string.Empty,
            request.NombreSustancia ?? string.Empty,
            request.Presentacion ?? string.Empty,
            request.Concentracion ?? string.Empty,
            request.EsControlado,
            request.NumeroLote ?? string.Empty,
            request.FechaCaducidad ?? string.Empty,
            request.CantidadInicial ?? string.Empty);

        // 2. MediatR: ValidationBehavior → RegistrarMedicamentoHandler
        var resultado = await sender.Send(command, cancellationToken);

        // 3. Dto → Response
        var response = new RegistrarMedicamentoResponse(
            resultado.IdMedicamento,
            resultado.NombreComercial,
            resultado.NombreSustancia,
            resultado.Presentacion,
            resultado.Concentracion,
            resultado.EsControlado,
            resultado.IdLote,
            resultado.NumeroLote,
            resultado.FechaCaducidad.ToString("yyyy-MM-dd"),
            resultado.Stock,
            resultado.SemaforoCaducidad,
            resultado.SeRegistroEntrada,
            resultado.Mensaje);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}

