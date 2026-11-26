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
        var loteCaducado = CrearLote(1, 20, Hoy.AddDays(-1));

        var existencias = ConStockMinimo(10).CalcularExistenciasVigentes([loteCaducado], Hoy);

        Assert.Equal(0, existencias.Vigentes);
        Assert.Equal(20, existencias.Caducadas);
        Assert.True(existencias.TieneLotesCaducados);
        Assert.True(existencias.EstaAgotado);
        Assert.False(existencias.EnStockBajo);
    }

    [Fact]
    public void Un_lote_caducado_no_se_suma_aunque_haya_otro_lote_vigente()
    {
        var lotes = new[]
        {
            CrearLote(1, 20, Hoy.AddDays(-1)),
            CrearLote(2, 5, Hoy.AddMonths(6)),
        };

        var existencias = ConStockMinimo(0).CalcularExistenciasVigentes(lotes, Hoy);

        Assert.Equal(5, existencias.Vigentes);
        Assert.Equal(20, existencias.Caducadas);
        Assert.True(existencias.TieneLotesCaducados);
    }

    [Fact]
    public void El_lote_que_caduca_hoy_ya_no_es_vigente_y_el_que_caduca_manana_si()
    {
        var lotes = new[]
        {
            CrearLote(1, 4, Hoy),
            CrearLote(2, 6, Hoy.AddDays(1)),
        };

        var existencias = ConStockMinimo(0).CalcularExistenciasVigentes(lotes, Hoy);

        Assert.Equal(6, existencias.Vigentes);
        Assert.Equal(4, existencias.Caducadas);
    }

    [Theory]
    [InlineData(10, 11, false, false)]
    [InlineData(10, 10, false, true)]
    [InlineData(10, 9, false, true)]
    [InlineData(10, 0, true, false)]
    [InlineData(0, 3, false, false)]
    public void Vigentes_contra_stock_minimo(int minimo, int vigentes, bool agotado, bool stockBajo)
    {
        var lotes = new List<Lote> { CrearLote(2, 50, Hoy.AddDays(-3)) };
        if (vigentes > 0)
        {
            lotes.Add(CrearLote(1, vigentes, Hoy.AddMonths(24)));
        }

        var existencias = ConStockMinimo(minimo).CalcularExistenciasVigentes(lotes, Hoy);

        Assert.Equal(vigentes, existencias.Vigentes);
        Assert.Equal(agotado, existencias.EstaAgotado);
        Assert.Equal(stockBajo, existencias.EnStockBajo);
        Assert.True(existencias.TieneLotesCaducados);
    }

    [Fact]
    public void Sin_lotes_caducados_no_se_marca_ninguna_alerta_de_caducidad()
    {
        var existencias = ConStockMinimo(5).CalcularExistenciasVigentes([CrearLote(1, 20, Hoy.AddMonths(6))], Hoy);

        Assert.Equal(0, existencias.Caducadas);
        Assert.False(existencias.TieneLotesCaducados);
    }

    [Fact]
    public void Los_lotes_sin_unidades_no_cuentan_ni_como_vigentes_ni_como_caducados()
    {
        var existencias = ConStockMinimo(5).CalcularExistenciasVigentes([CrearLote(1, 0, Hoy.AddDays(-1))], Hoy);

        Assert.Equal(0, existencias.Vigentes);
        Assert.Equal(0, existencias.Caducadas);
        Assert.False(existencias.TieneLotesCaducados);
    }

    [Fact]
    public void Las_existencias_generales_siguen_contando_los_lotes_caducados_con_unidades()
    {
        var existencias = ConStockMinimo(5).CalcularExistencias([CrearLote(1, 20, Hoy.AddDays(-1))], Hoy);

        Assert.Equal(20, existencias.Total);
        Assert.Equal("Rojo", existencias.SemaforoGeneral);
        Assert.False(existencias.EstaAgotado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(250)]
    public void Actualizar_stock_minimo_acepta_enteros_de_cero_o_mas(int valor)
    {
        var medicamento = ConStockMinimo(3);

        medicamento.ActualizarStockMinimo(valor);

        Assert.Equal(valor, medicamento.StockMinimo);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Actualizar_stock_minimo_rechaza_negativos_y_conserva_el_valor(int valor)
    {
        var medicamento = ConStockMinimo(3);

        Assert.Throws<EtbotFarma.Domain.Common.DomainException>(() => medicamento.ActualizarStockMinimo(valor));

        Assert.Equal(3, medicamento.StockMinimo);
    }
}
