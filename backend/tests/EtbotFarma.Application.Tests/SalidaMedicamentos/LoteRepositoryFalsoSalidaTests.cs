using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.SalidaMedicamentos;

public class LoteRepositoryFalsoSalidaTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 26);

    private static LoteRepositoryFalso ConLotes()
    {
        var repositorio = new LoteRepositoryFalso();
        repositorio.Lotes.Add(Lote.Reconstruir(8, 1, "L-8", Hoy.AddMonths(4), 10, Hoy.AddMonths(-6)));
        repositorio.Lotes.Add(Lote.Reconstruir(5, 1, "L-5", Hoy.AddMonths(4), 10, Hoy.AddMonths(-1)));
        repositorio.Lotes.Add(Lote.Reconstruir(2, 1, "L-2", Hoy.AddMonths(5), 10, Hoy.AddMonths(-9)));
        repositorio.Lotes.Add(Lote.Reconstruir(1, 1, "L-1", Hoy.AddMonths(6), 10, Hoy.AddMonths(-9)));
        repositorio.Lotes.Add(Lote.Reconstruir(3, 2, "L-3", Hoy.AddMonths(1), 10, Hoy.AddMonths(-9)));
        return repositorio;
    }

    [Fact]
    public async Task Los_lotes_para_salida_salen_por_caducidad_y_despues_por_id()
    {
        var repositorio = ConLotes();

        var lotes = await repositorio.ObtenerPorMedicamentoParaActualizarAsync(1, CancellationToken.None);

        Assert.Equal(new[] { 5, 8, 2, 1 }, lotes.Select(l => l.Id));
    }

    [Fact]
    public async Task La_consulta_posterior_refleja_el_stock_descontado()
    {
        var repositorio = ConLotes();

        Assert.True(await repositorio.DescontarStockAsync(5, 4, CancellationToken.None));

        var lotes = await repositorio.ObtenerPorMedicamentoParaActualizarAsync(1, CancellationToken.None);
        Assert.Equal(6, lotes.Single(l => l.Id == 5).Stock);
        Assert.Equal(10, lotes.Single(l => l.Id == 8).Stock);
    }

    [Fact]
    public async Task Descontar_mas_que_el_stock_o_un_lote_inexistente_no_cambia_nada()
    {
        var repositorio = ConLotes();

        Assert.False(await repositorio.DescontarStockAsync(5, 11, CancellationToken.None));
        Assert.False(await repositorio.DescontarStockAsync(99, 1, CancellationToken.None));

        Assert.Empty(repositorio.Descuentos);
        Assert.Equal(10, repositorio.Lotes.Single(l => l.Id == 5).Stock);
    }

    [Fact]
    public async Task El_falso_conserva_los_metodos_que_usa_la_baja_de_lotes()
    {
        var repositorio = ConLotes();

        var lote = await repositorio.ObtenerPorIdParaActualizarAsync(5, CancellationToken.None);

        Assert.NotNull(lote);
        Assert.Equal(5, lote!.Id);
    }
}
