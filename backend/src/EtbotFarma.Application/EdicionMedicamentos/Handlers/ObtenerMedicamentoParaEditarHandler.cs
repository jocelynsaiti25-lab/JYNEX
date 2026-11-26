using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EdicionMedicamentos.Dtos;
using EtbotFarma.Application.EdicionMedicamentos.Queries;

namespace EtbotFarma.Application.EdicionMedicamentos.Handlers;

/// <summary>
/// HU-09 escenario 2: devuelve los datos descriptivos actuales del medicamento para precargar el formulario.
/// Las existencias y el semáforo se incluyen SOLO como referencia (escenario 10): no se editan aquí.
/// </summary>
public sealed class ObtenerMedicamentoParaEditarHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IDateTimeProvider reloj) : IQueryHandler<ObtenerMedicamentoParaEditarQuery, MedicamentoParaEditarDto>
{
    public async Task<MedicamentoParaEditarDto> Handle(
        ObtenerMedicamentoParaEditarQuery query,
        CancellationToken cancellationToken)
    {
        // 1. El medicamento debe existir (dirección escrita a mano con un id inválido)
        var medicamento = await medicamentos.ObtenerPorIdAsync(query.IdMedicamento, cancellationToken)
            ?? throw new NoEncontradoException("El medicamento no existe");

        // 2. Existencias actuales, solo para mostrarlas de referencia (bloqueadas en el formulario)
        var hoy = DateOnly.FromDateTime(reloj.UtcNow.UtcDateTime);
        var lotesDelMedicamento = await lotes.ObtenerPorMedicamentosAsync([medicamento.Id], cancellationToken);
        var existencias = medicamento.CalcularExistencias(lotesDelMedicamento, hoy);

        return new MedicamentoParaEditarDto(
            medicamento.Id,
            medicamento.NombreComercial,
            medicamento.NombreSustancia,
            medicamento.Presentacion,
            medicamento.Concentracion,
            medicamento.EsControlado,
            existencias.Total,
            existencias.SemaforoGeneral);
    }
}
