using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Domain.ValueObjects;

namespace EtbotFarma.Domain.Tests.Entities;

public class SalidaMedicamentosDomainTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 26);

    private static Lote LoteDePrueba(int id, DateOnly caducidad, int stock, DateOnly? entrada = null) =>
        Lote.Reconstruir(id, 1, $"L{id:000}", caducidad, stock, entrada ?? new DateOnly(2026, 1, 1));

    [Fact]
    public void Con_un_solo_lote_suficiente_lo_usa_solo_a_el()
    {
        var propuesta = PropuestaFefo.Calcular([LoteDePrueba(1, new DateOnly(2027, 5, 1), 20)], 8, Hoy);

        var asignado = Assert.Single(propuesta.Lotes);
        Assert.Equal(1, asignado.Lote.Id);
        Assert.Equal(8, asignado.Cantidad);
        Assert.True(propuesta.Alcanza);
    }

    [Fact]
    public void Reparte_en_orden_FEFO_y_no_toca_lotes_posteriores()
    {
        var lotes = new[]
        {
            LoteDePrueba(3, new DateOnly(2027, 1, 15), 20),
            LoteDePrueba(1, new DateOnly(2026, 12, 10), 5),
            LoteDePrueba(2, new DateOnly(2026, 12, 20), 10)
        };

        var propuesta = PropuestaFefo.Calcular(lotes, 12, Hoy);

        Assert.Equal(2, propuesta.Lotes.Count);
        Assert.Equal((1, 5), (propuesta.Lotes[0].Lote.Id, propuesta.Lotes[0].Cantidad));
        Assert.Equal((2, 7), (propuesta.Lotes[1].Lote.Id, propuesta.Lotes[1].Cantidad));
        Assert.Equal(35, propuesta.CantidadDisponible);
    }

    [Fact]
    public void Con_la_misma_caducidad_sale_primero_el_que_entro_antes_y_luego_el_de_menor_id()
    {
        var caducidad = new DateOnly(2027, 3, 1);
        var lotes = new[]
        {
            LoteDePrueba(9, caducidad, 5, new DateOnly(2026, 2, 1)),
            LoteDePrueba(7, caducidad, 5, new DateOnly(2026, 2, 1)),
            LoteDePrueba(8, caducidad, 5, new DateOnly(2026, 1, 1))
        };

        var propuesta = PropuestaFefo.Calcular(lotes, 15, Hoy);

        Assert.Equal(new[] { 8, 7, 9 }, propuesta.Lotes.Select(a => a.Lote.Id));
    }

    [Fact]
    public void Excluye_lotes_caducados_los_que_caducan_hoy_y_los_que_no_tienen_existencias()
    {
        var lotes = new[]
        {
            LoteDePrueba(1, Hoy.AddDays(-1), 50),
            LoteDePrueba(2, Hoy, 50),
            LoteDePrueba(3, new DateOnly(2027, 1, 1), 0),
            LoteDePrueba(4, new DateOnly(2027, 6, 1), 10)
        };

        var propuesta = PropuestaFefo.Calcular(lotes, 10, Hoy);

        Assert.Equal(10, propuesta.CantidadDisponible);
        Assert.Equal(4, Assert.Single(propuesta.Lotes).Lote.Id);
    }

    [Fact]
    public void Con_existencias_insuficientes_no_alcanza()
    {
        var propuesta = PropuestaFefo.Calcular([LoteDePrueba(1, new DateOnly(2027, 1, 1), 4)], 5, Hoy);

        Assert.False(propuesta.Alcanza);
        Assert.Equal(4, propuesta.CantidadDisponible);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Una_cantidad_cero_o_negativa_se_rechaza(int cantidad)
    {
        Assert.Throws<DomainException>(() => PropuestaFefo.Calcular([], cantidad, Hoy));
    }

    [Fact]
    public void El_movimiento_de_salida_suma_los_detalles_y_guarda_usuario_y_fecha()
    {
        var fecha = new DateTime(2026, 11, 26, 15, 0, 0, DateTimeKind.Utc);

        var movimiento = Movimiento.CrearSalida(1, 7, [(10, 5), (11, 7)], fecha);

        Assert.Equal(Movimiento.TipoSalida, movimiento.Tipo);
        Assert.Equal(12, movimiento.CantidadTotal);
        Assert.Equal(7, movimiento.IdUsuario);
        Assert.Equal(fecha, movimiento.Fecha);
        Assert.Equal(new[] { (10, 5), (11, 7) }, movimiento.Detalles.Select(d => (d.IdLote, d.Cantidad)));
    }

    [Fact]
    public void El_movimiento_de_salida_sin_lotes_o_con_cantidades_invalidas_no_se_crea()
    {
        Assert.Throws<DomainException>(() => Movimiento.CrearSalida(1, 7, [], DateTime.UtcNow));
        Assert.Throws<DomainException>(() => Movimiento.CrearSalida(1, 7, [(10, 0)], DateTime.UtcNow));
    }
}
