using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.MedicamentosAgotados.Dtos;
using EtbotFarma.Application.MedicamentosAgotados.Queries;

namespace EtbotFarma.Application.MedicamentosAgotados.Handlers;

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
        if (usuarioActual.IdUsuario is null)
            throw new NoAutenticadoException();

        var hoy = reloj.Hoy;
        var listaMedicamentos = await medicamentos.ObtenerTodosActivosAsync(cancellationToken);
        var idsMedicamento = listaMedicamentos.Select(m => m.Id).ToList();
        var todosLosLotes = await lotes.ObtenerPorMedicamentosAsync(idsMedicamento, cancellationToken);
        var lotesPorMedicamento = todosLosLotes.ToLookup(l => l.IdMedicamento);

        var termino = query.Termino?.Trim();

        return listaMedicamentos
            .Where(m => m.CalcularExistenciasVigentes(lotesPorMedicamento[m.Id], hoy).EstaAgotado)
            .Where(m => string.IsNullOrWhiteSpace(termino)
                || m.NombreComercial.Contains(termino, StringComparison.OrdinalIgnoreCase)
                || m.NombreSustancia.Contains(termino, StringComparison.OrdinalIgnoreCase))
            .Select(m => new MedicamentoAgotadoDto(
                m.Id,
                m.NombreComercial,
                m.NombreSustancia,
                m.Presentacion,
                m.Concentracion,
                MedicamentoAgotadoDto.EtiquetaSinExistencias))
            .ToList();
    }
}
