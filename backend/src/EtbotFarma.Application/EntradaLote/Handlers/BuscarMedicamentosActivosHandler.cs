using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EntradaLote.Dtos;
using EtbotFarma.Application.EntradaLote.Queries;
using EtbotFarma.Domain.Enums;

namespace EtbotFarma.Application.EntradaLote.Handlers;

/// <summary>
/// HU-07 escenarios 1, 2 y 11: devuelve medicamentos activos con sus existencias y semáforo actual.
/// </summary>
public sealed class BuscarMedicamentosActivosHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj) : IQueryHandler<BuscarMedicamentosActivosQuery, IReadOnlyList<MedicamentoActivoDto>>
{
    public async Task<IReadOnlyList<MedicamentoActivoDto>> Handle(
        BuscarMedicamentosActivosQuery query,
        CancellationToken cancellationToken)
    {
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        var hoy = DateOnly.FromDateTime(reloj.UtcNow.UtcDateTime);
        var lista = await medicamentos.BuscarActivosAsync(query.Termino, cancellationToken);
        var resultado = new List<MedicamentoActivoDto>(lista.Count);

        foreach (var med in lista)
        {
            var lotesMed = await lotes.ObtenerPorMedicamentoAsync(med.Id, cancellationToken);
            var lotesConStock = lotesMed.Where(l => l.Stock > 0).ToList();
            var existencias = lotesConStock.Sum(l => l.Stock);

            var semaforo = CalcularSemaforoGeneral(lotesConStock.Select(l => l.ObtenerSemaforo(hoy)));
            var estaAgotado = existencias == 0;
            var enStockBajo = !estaAgotado && med.StockMinimo > 0 && existencias <= med.StockMinimo;

            resultado.Add(new MedicamentoActivoDto(
                med.Id,
                med.NombreComercial,
                med.NombreSustancia,
                med.Presentacion,
                med.Concentracion,
                med.EsControlado,
                med.StockMinimo,
                existencias,
                semaforo,
                estaAgotado,
                enStockBajo));
        }

        return resultado;
    }

    private static string CalcularSemaforoGeneral(IEnumerable<SemaforoCaducidad> semaforos)
    {
        var lista = semaforos.ToList();
        if (lista.Count == 0) return "SinStock";
        if (lista.Contains(SemaforoCaducidad.Rojo)) return SemaforoCaducidad.Rojo.ToString();
        if (lista.Contains(SemaforoCaducidad.Amarillo)) return SemaforoCaducidad.Amarillo.ToString();
        return SemaforoCaducidad.Verde.ToString();
    }
}