using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.Inventario.Dtos;
using EtbotFarma.Application.Inventario.Queries;

namespace EtbotFarma.Application.Inventario.Handlers;

/// <summary>
/// HU-08: Coordina la búsqueda de medicamentos y lotes en el inventario.
/// Operación de solo lectura (sin IUnitOfWork ni modificación de datos).
/// </summary>
public sealed class ConsultarInventarioHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj) : IQueryHandler<ConsultarInventarioQuery, IReadOnlyList<ItemInventarioDto>>
{
    public async Task<IReadOnlyList<ItemInventarioDto>> Handle(
        ConsultarInventarioQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Verificar sesión activa (HU-08 escenario 8)
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        // 2. Obtener medicamentos activos y lotes ordenados por fecha de caducidad ascendente (HU-08 escenario 4)
        var hoy = DateOnly.FromDateTime(reloj.UtcNow.UtcDateTime);
        var listaMedicamentos = await medicamentos.ObtenerTodosActivosAsync(cancellationToken);
        var todosLosLotes = await lotes.ObtenerTodosOrdenadosPorCaducidadAsync(cancellationToken);

        var lotesPorMedicamento = todosLosLotes
            .Where(l => l.Stock > 0)
            .OrderBy(l => l.FechaCaducidad) // HU-08 escenario 4: caducidad ascendente
            .GroupBy(l => l.IdMedicamento)
            .ToDictionary(g => g.Key, g => g.ToList());

        var filas = new List<ItemInventarioDto>();

        foreach (var med in listaMedicamentos)
        {
            var stockTotalMedicamento = lotesPorMedicamento.TryGetValue(med.Id, out var lotesMed)
                ? lotesMed.Sum(l => l.Stock)
                : 0;

            var estadoDisponibilidad = DeterminarEstadoDisponibilidad(stockTotalMedicamento, med.StockMinimo);

            if (lotesMed is { Count: > 0 })
            {
                foreach (var lote in lotesMed)
                {
                    var semaforo = lote.ObtenerSemaforo(hoy).ToString();
                    filas.Add(new ItemInventarioDto(
                        med.Id,
                        lote.Id,
                        med.NombreComercial,
                        med.NombreSustancia,
                        med.Presentacion,
                        med.Concentracion,
                        med.EsControlado,
                        med.StockMinimo,
                        lote.NumeroLote,
                        lote.Stock,
                        lote.FechaCaducidad,
                        semaforo,
                        estadoDisponibilidad));
                }
            }
            else
            {
                // Medicamento activo sin lotes con stock (Agotado)
                filas.Add(new ItemInventarioDto(
                    med.Id,
                    null,
                    med.NombreComercial,
                    med.NombreSustancia,
                    med.Presentacion,
                    med.Concentracion,
                    med.EsControlado,
                    med.StockMinimo,
                    "Sin lote",
                    0,
                    null,
                    "SinStock",
                    "Agotado"));
            }
        }

        // 3. Aplicar filtro de semáforo de caducidad si está activo (HU-08 escenario 5)
        var semaforoFiltro = query.Semaforo?.Trim();
        if (!string.IsNullOrWhiteSpace(semaforoFiltro)
            && !semaforoFiltro.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            filas = filas
                .Where(f => f.SemaforoCaducidad.Equals(semaforoFiltro, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // 4. Aplicar búsqueda insensible a mayúsculas por nombre, sustancia o lote (HU-08 escenarios 2, 5 y 7)
        var termino = query.Termino?.Trim();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            filas = filas
                .Where(f =>
                    f.NombreComercial.Contains(termino, StringComparison.OrdinalIgnoreCase)
                    || f.NombreSustancia.Contains(termino, StringComparison.OrdinalIgnoreCase)
                    || f.NumeroLote.Contains(termino, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return filas;
    }

    private static string DeterminarEstadoDisponibilidad(int stockTotal, int stockMinimo)
    {
        if (stockTotal == 0) return "Agotado";
        if (stockMinimo > 0 && stockTotal <= stockMinimo) return "Stock bajo";
        return "Disponible";
    }
}