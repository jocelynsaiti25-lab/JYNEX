using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

public class MedicamentosAgotadosDomainTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 25);

    private static Medicamento CrearMedicamento() =>
        Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 10, true, DateTime.UtcNow);

    private static Lote LoteDe(int id, DateOnly caducidad, int stock) =>
        Lote.Reconstruir(id, 1, $"L-{id}", caducidad, stock, Hoy.AddMonths(-6));

    [Fact]
    public void Un_lote_rojo_que_aun_no_caduca_cuenta_como_existencia_vigente()
    {
        var lote = LoteDe(1, Hoy.AddMonths(2), 12);

        var existencias = CrearMedicamento().CalcularExistenciasVigentes([lote], Hoy);

        Assert.Equal(12, existencias.Vigentes);
        Assert.False(existencias.EstaAgotado);
    }

    [Fact]
    public void Con_un_lote_vigente_y_otro_caducado_las_vigentes_son_solo_las_del_vigente()
    {
        var existencias = CrearMedicamento().CalcularExistenciasVigentes(
            [LoteDe(1, Hoy.AddDays(-2), 30), LoteDe(2, Hoy.AddMonths(9), 4)], Hoy);

        Assert.Equal(4, existencias.Vigentes);
        Assert.Equal(30, existencias.Caducadas);
        Assert.False(existencias.EstaAgotado);
    }

    [Fact]
    public void Con_stock_minimo_y_cero_vigentes_se_identifica_como_agotado_y_no_como_stock_bajo()
    {
        var existencias = CrearMedicamento().CalcularExistenciasVigentes([LoteDe(1, Hoy.AddDays(-1), 50)], Hoy);

        Assert.True(existencias.EstaAgotado);
        Assert.False(existencias.EnStockBajo);
    }

    [Fact]
    public void Un_lote_con_cero_unidades_no_incorpora_existencias_vigentes()
    {
        var existencias = CrearMedicamento().CalcularExistenciasVigentes([LoteDe(1, Hoy.AddMonths(12), 0)], Hoy);

        Assert.Equal(0, existencias.Vigentes);
        Assert.True(existencias.EstaAgotado);
    }

    [Fact]
    public void Al_pasar_la_fecha_de_caducidad_el_unico_lote_vigente_deja_el_medicamento_agotado()
    {
        var lote = LoteDe(1, Hoy.AddDays(3), 10);

        Assert.False(CrearMedicamento().CalcularExistenciasVigentes([lote], Hoy).EstaAgotado);
        Assert.True(CrearMedicamento().CalcularExistenciasVigentes([lote], Hoy.AddDays(3)).EstaAgotado);
    }
}
