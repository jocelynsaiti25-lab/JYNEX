using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

public class MedicamentoExistenciasTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 25);

    private static Medicamento ConStockMinimo(int stockMinimo) =>
        Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, stockMinimo, true, DateTime.UtcNow);

    private static Lote CrearLote(int id, int stock, DateOnly caducidad) =>
        Lote.Reconstruir(id, 1, $"L-{id}", caducidad, stock, Hoy);

    [Fact]
    public void Sin_lotes_esta_agotado_y_sin_stock()
    {
        var existencias = ConStockMinimo(10).CalcularExistencias([], Hoy);

        Assert.Equal(0, existencias.Total);
        Assert.Equal("SinStock", existencias.SemaforoGeneral);
        Assert.True(existencias.EstaAgotado);
        Assert.False(existencias.EnStockBajo);
    }

    [Fact]
    public void Los_lotes_sin_unidades_no_cuentan()
    {
        var existencias = ConStockMinimo(10).CalcularExistencias([CrearLote(1, 0, Hoy.AddMonths(2))], Hoy);

        Assert.True(existencias.EstaAgotado);
        Assert.Equal("SinStock", existencias.SemaforoGeneral);
    }

    [Fact]
    public void El_semaforo_general_es_el_del_lote_mas_cercano_a_caducar()
    {
        var lotes = new[]
        {
            CrearLote(1, 5, Hoy.AddMonths(24)),
            CrearLote(2, 5, Hoy.AddMonths(8)),
            CrearLote(3, 5, Hoy.AddMonths(2)),
        };

        var existencias = ConStockMinimo(0).CalcularExistencias(lotes, Hoy);

        Assert.Equal(15, existencias.Total);
        Assert.Equal("Rojo", existencias.SemaforoGeneral);
    }

    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(10, 11, false)]
    [InlineData(0, 3, false)]
    public void Stock_bajo_cuando_las_existencias_no_superan_el_minimo(int minimo, int existencias, bool esperado)
    {
        var resultado = ConStockMinimo(minimo).CalcularExistencias([CrearLote(1, existencias, Hoy.AddMonths(24))], Hoy);

        Assert.Equal(esperado, resultado.EnStockBajo);
        Assert.False(resultado.EstaAgotado);
    }

    [Fact]
    public void Un_lote_caducado_con_unidades_no_cuenta_como_existencia_vigente()
    {
        // HU-11: "existencias vigentes" excluye lotes caducados aunque todavía tengan unidades
        var loteCaducado = CrearLote(1, 20, Hoy.AddDays(-1));

        var existencias = ConStockMinimo(10).CalcularExistencias([loteCaducado], Hoy);

        Assert.Equal(0, existencias.Total);
        Assert.Equal("SinStock", existencias.SemaforoGeneral);
        Assert.True(existencias.EstaAgotado);
    }

    [Fact]
    public void Un_lote_caducado_no_se_suma_aunque_haya_otro_lote_vigente()
    {
        var lotes = new[]
        {
            CrearLote(1, 20, Hoy.AddDays(-1)), // caducado: no cuenta
            CrearLote(2, 5, Hoy.AddMonths(6)), // vigente
        };

        var existencias = ConStockMinimo(0).CalcularExistencias(lotes, Hoy);

        Assert.Equal(5, existencias.Total);
    }
}
