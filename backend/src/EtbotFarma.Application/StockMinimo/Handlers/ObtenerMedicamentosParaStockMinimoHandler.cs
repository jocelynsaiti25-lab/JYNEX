using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.StockMinimo.Dtos;
using EtbotFarma.Application.StockMinimo.Queries;

namespace EtbotFarma.Application.StockMinimo.Handlers;

/// <summary>
/// HU-11 escenarios 1 al 4: coordina la lista de medicamentos con su stock mínimo y alertas.
/// Operación de solo lectura; usa Medicamento.CalcularExistenciasVigentes del Domain, sin
/// repetir la regla de stock bajo/agotado.
/// </summary>
public sealed class ObtenerMedicamentosParaStockMinimoHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj) : IQueryHandler<ObtenerMedicamentosParaStockMinimoQuery, IReadOnlyList<MedicamentoStockDto>>
{
    public async Task<IReadOnlyList<MedicamentoStockDto>> Handle(
        ObtenerMedicamentosParaStockMinimoQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Verificar sesión activa (Administrador o Empleado, por igual)
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        // 2. Obtener medicamentos activos y sus lotes
        var hoy = reloj.Hoy;
        var listaMedicamentos = await medicamentos.ObtenerTodosActivosAsync(cancellationToken);
        var idsMedicamento = listaMedicamentos.Select(m => m.Id).ToList();
        var todosLosLotes = await lotes.ObtenerPorMedicamentosAsync(idsMedicamento, cancellationToken);

        var lotesPorMedicamento = todosLosLotes
            .GroupBy(l => l.IdMedicamento)
            .ToDictionary(g => g.Key, g => (IEnumerable<Domain.Entities.Lote>)g);

        // 3. Calcular existencias vigentes y alertas (escenarios 2, 3 y 4) por medicamento
        return listaMedicamentos
            .Select(med =>
            {
                lotesPorMedicamento.TryGetValue(med.Id, out var lotesMed);
                var existencias = med.CalcularExistenciasVigentes(lotesMed ?? [], hoy);

                return new MedicamentoStockDto(
                    med.Id,
                    med.NombreComercial,
                    med.NombreSustancia,
                    med.Presentacion,
                    med.Concentracion,
                    med.StockMinimo,
                    existencias.Vigentes,
                    existencias.Caducadas,
                    existencias.TieneLotesCaducados,
                    existencias.EnStockBajo,
                    existencias.EstaAgotado);
            })
            .ToList();
    }
}
