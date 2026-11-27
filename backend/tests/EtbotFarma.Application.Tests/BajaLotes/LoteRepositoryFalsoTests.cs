using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.BajaLotes;

public class LoteRepositoryFalsoTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 26);

    private static LoteRepositoryFalso ConLote(int stock, bool simularSinFilas = false)
    {
        var repositorio = new LoteRepositoryFalso { SimularDescuentoSinFilas = simularSinFilas };
        repositorio.Lotes.Add(Lote.Reconstruir(5, 1, "L-5", Hoy.AddMonths(1), stock, Hoy.AddMonths(-3)));
        repositorio.Lotes.Add(Lote.Reconstruir(6, 1, "L-6", Hoy.AddMonths(12), 30, Hoy.AddMonths(-3)));
        return repositorio;
    }

    [Fact]
    public async Task Un_descuento_exitoso_actualiza_el_stock_y_lo_registra()
    {
        var repositorio = ConLote(40);

        var resultado = await repositorio.DescontarStockAsync(5, 15, CancellationToken.None);

        Assert.True(resultado);
        Assert.Equal(25, repositorio.Lotes.Single(l => l.Id == 5).Stock);
        Assert.Equal(30, repositorio.Lotes.Single(l => l.Id == 6).Stock);
        Assert.Equal((5, 15), Assert.Single(repositorio.Descuentos));
    }

    [Fact]
    public async Task Descontar_todo_el_stock_lo_deja_en_cero()
    {
        var repositorio = ConLote(40);

        Assert.True(await repositorio.DescontarStockAsync(5, 40, CancellationToken.None));

        Assert.Equal(0, repositorio.Lotes.Single(l => l.Id == 5).Stock);
    }

    [Fact]
    public async Task Un_lote_inexistente_no_cambia_nada()
    {
        var repositorio = ConLote(40);

        var resultado = await repositorio.DescontarStockAsync(99, 5, CancellationToken.None);

        Assert.False(resultado);
        Assert.Empty(repositorio.Descuentos);
        Assert.Equal(40, repositorio.Lotes.Single(l => l.Id == 5).Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(41)]
    public async Task Una_cantidad_invalida_o_mayor_al_stock_no_descuenta(int cantidad)
    {
        var repositorio = ConLote(40);

        var resultado = await repositorio.DescontarStockAsync(5, cantidad, CancellationToken.None);

        Assert.False(resultado);
        Assert.Empty(repositorio.Descuentos);
        Assert.Equal(40, repositorio.Lotes.Single(l => l.Id == 5).Stock);
    }

    [Fact]
    public async Task Simular_descuento_sin_filas_no_descuenta_ni_registra()
    {
        var repositorio = ConLote(40, simularSinFilas: true);

        var resultado = await repositorio.DescontarStockAsync(5, 10, CancellationToken.None);

        Assert.False(resultado);
        Assert.Empty(repositorio.Descuentos);
        Assert.Equal(40, repositorio.Lotes.Single(l => l.Id == 5).Stock);
    }

    [Fact]
    public async Task Obtener_para_actualizar_devuelve_una_copia_como_lo_haria_la_base()
    {
        var repositorio = ConLote(40);

        var lote = await repositorio.ObtenerPorIdParaActualizarAsync(5, CancellationToken.None);
        lote!.DarDeBaja(Hoy, 40);

        Assert.Equal(40, repositorio.Lotes.Single(l => l.Id == 5).Stock);
    }
}
