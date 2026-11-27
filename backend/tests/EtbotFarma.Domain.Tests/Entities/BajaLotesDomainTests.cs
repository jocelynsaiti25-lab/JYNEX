using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

public class BajaLotesDomainTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 26);

    private static Lote LoteQueCaduca(DateOnly fechaCaducidad, int stock = 40) =>
        Lote.Reconstruir(5, 1, "L-2301", fechaCaducidad, stock, new DateOnly(2026, 1, 10));

    [Fact]
    public void Un_lote_rojo_con_existencias_se_da_de_baja_y_queda_en_cero()
    {
        var lote = LoteQueCaduca(Hoy.AddMonths(2));

        var retirada = lote.DarDeBaja(Hoy, 40);

        Assert.Equal(40, retirada);
        Assert.Equal(0, lote.Stock);
    }

    [Fact]
    public void Un_lote_ya_caducado_tambien_se_puede_dar_de_baja()
    {
        var lote = LoteQueCaduca(Hoy.AddDays(-10), 15);

        Assert.True(lote.PuedeDarseDeBaja(Hoy));
        Assert.Equal(15, lote.DarDeBaja(Hoy, 15));
        Assert.Equal(0, lote.Stock);
    }

    [Fact]
    public void Un_lote_que_caduca_hoy_se_puede_dar_de_baja()
    {
        Assert.True(LoteQueCaduca(Hoy).PuedeDarseDeBaja(Hoy));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(24)]
    public void Un_lote_amarillo_o_verde_no_se_puede_dar_de_baja(int meses)
    {
        var lote = LoteQueCaduca(Hoy.AddMonths(meses));

        Assert.False(lote.PuedeDarseDeBaja(Hoy));
        var ex = Assert.Throws<DomainException>(() => lote.DarDeBaja(Hoy, 40));
        Assert.Equal("El lote ya no es elegible.", ex.Message);
        Assert.Equal(40, lote.Stock);
    }

    [Fact]
    public void En_el_limite_de_tres_meses_es_rojo_y_un_dia_despues_ya_no()
    {
        Assert.True(LoteQueCaduca(Hoy.AddMonths(3)).PuedeDarseDeBaja(Hoy));
        Assert.False(LoteQueCaduca(Hoy.AddMonths(3).AddDays(1)).PuedeDarseDeBaja(Hoy));
    }

    [Fact]
    public void Un_lote_rojo_sin_existencias_no_se_puede_dar_de_baja()
    {
        var lote = LoteQueCaduca(Hoy.AddMonths(1), 0);

        Assert.False(lote.PuedeDarseDeBaja(Hoy));
        Assert.Throws<DomainException>(() => lote.DarDeBaja(Hoy, 1));
    }

    [Theory]
    [InlineData(39)]
    [InlineData(41)]
    public void Si_las_existencias_cambiaron_desde_la_consulta_el_lote_ya_no_es_elegible(int cantidadConsultada)
    {
        var lote = LoteQueCaduca(Hoy.AddMonths(1));

        var ex = Assert.Throws<DomainException>(() => lote.DarDeBaja(Hoy, cantidadConsultada));

        Assert.Equal("El lote ya no es elegible.", ex.Message);
        Assert.Equal(40, lote.Stock);
    }

    [Fact]
    public void El_movimiento_de_baja_guarda_tipo_motivo_usuario_fecha_y_detalle_del_lote()
    {
        var fecha = new DateTime(2026, 11, 26, 15, 30, 0, DateTimeKind.Utc);

        var movimiento = Movimiento.CrearBaja(1, 5, 7, 40, "  Próximo a caducar  ", fecha);

        Assert.Equal(Movimiento.TipoBaja, movimiento.Tipo);
        Assert.Equal("Próximo a caducar", movimiento.Motivo);
        Assert.Equal(7, movimiento.IdUsuario);
        Assert.Equal(40, movimiento.CantidadTotal);
        Assert.Equal(fecha, movimiento.Fecha);
        var detalle = Assert.Single(movimiento.Detalles);
        Assert.Equal(5, detalle.IdLote);
        Assert.Equal(40, detalle.Cantidad);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void El_movimiento_de_baja_sin_motivo_no_se_crea(string motivo)
    {
        var ex = Assert.Throws<DomainException>(() => Movimiento.CrearBaja(1, 5, 7, 40, motivo, DateTime.UtcNow));
        Assert.Equal("El motivo es obligatorio.", ex.Message);
    }

    [Fact]
    public void El_movimiento_de_baja_acepta_200_caracteres_y_rechaza_201()
    {
        Movimiento.CrearBaja(1, 5, 7, 40, new string('a', 200), DateTime.UtcNow);

        var ex = Assert.Throws<DomainException>(() =>
            Movimiento.CrearBaja(1, 5, 7, 40, new string('a', 201), DateTime.UtcNow));
        Assert.Equal("El motivo no puede tener más de 200 caracteres.", ex.Message);
    }
}
