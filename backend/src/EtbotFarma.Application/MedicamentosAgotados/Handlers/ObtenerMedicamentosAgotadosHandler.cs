using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.MedicamentosAgotados.Dtos;
using EtbotFarma.Application.MedicamentosAgotados.Queries;

namespace EtbotFarma.Application.MedicamentosAgotados.Handlers;

/// <summary>
/// HU-12: coordina la lista de medicamentos agotados. Usa Medicamento.CalcularExistenciasVigentes
/// del Domain (igual que HU-11) para que un lote caducado nunca cuente como existencia, sin
/// repetir la regla de "agotado".
/// </summary>
public sealed class ObtenerMedicamentosAgotadosHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IUsuarioActual usuarioActual,
    IDateTimeProvider reloj) : IQueryHandler<ObtenerMedicamentosAgotadosQuery, IReadOnlyList<MedicamentoAgotadoDto>>
{
    public async Task<IReadOnlyList<MedicamentoAgotadoDto>> Handle(
        ObtenerMedicamentosAgotadosQuery query,
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

        // 3. Filtrar los que no tienen ninguna existencia vigente (escenario 2), sin importar
        //    su semáforo de caducidad (escenario 1: independiente de ese filtro)
        return listaMedicamentos
            .Where(med =>
            {
                lotesPorMedicamento.TryGetValue(med.Id, out var lotesMed);
                return med.CalcularExistenciasVigentes(lotesMed ?? [], hoy).EstaAgotado;
            })
            .Select(med => new MedicamentoAgotadoDto(
                med.Id,
                med.NombreComercial,
                med.NombreSustancia,
                med.Presentacion,
                med.Concentracion))
            .ToList();
    }
}
