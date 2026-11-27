using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Domain.ValueObjects;

namespace EtbotFarma.Domain.Tests.ValueObjects;

public class PropuestaFefoTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 25);

    private static Lote LoteDe(int id, DateOnly caducidad, int stock, DateOnly? entrada = null)
        => Lote.Reconstruir(id, 1, $"L-{id}", caducidad, stock, entrada ?? Hoy.AddMonths(-6));

    [Fact]
    public void Toma_primero_el_lote_que_caduca_antes()
    {
        var lotes = new[]
        {
            LoteDe(1, Hoy.AddMonths(10), 50),
            LoteDe(2, Hoy.AddMonths(4), 30)
        };

        var propuesta = PropuestaFefo.Calcular(lotes, 20, Hoy);

        var asignacion = Assert.Single(propuesta.Asignaciones);
        Assert.Equal(2, asignacion.IdLote);
        Assert.Equal(20, asignacion.Cantidad);
    }

    [Fact]
    public void Reparte_la_salida_entre_varios_lotes_en_orden_de_caducidad()
    {
        var lotes = new[]
        {
            LoteDe(1, Hoy.AddMonths(10), 50),
            LoteDe(2, Hoy.AddMonths(4), 30),
            LoteDe(3, Hoy.AddMonths(6), 10)
        };

        var propuesta = PropuestaFefo.Calcular(lotes, 60, Hoy);

        Assert.Equal(new[] { 2, 3, 1 }, propuesta.Asignaciones.Select(a => a.IdLote));
        Assert.Equal(new[] { 30, 10, 20 }, propuesta.Asignaciones.Select(a => a.Cantidad));
        Assert.Equal(60, propuesta.CantidadTotal);
        Assert.Equal(90, propuesta.ExistenciasVigentes);
    }

    [Fact]
    public void Con_misma_caducidad_desempata_por_id_ascendente_sin_usar_la_fecha_de_entrada()
    {
        var caducidad = Hoy.AddMonths(5);
        var lotes = new[]
        {
            LoteDe(8, caducidad, 10, Hoy.AddMonths(-6)),
            LoteDe(5, caducidad, 10, Hoy.AddMonths(-1)),
            LoteDe(2, Hoy.AddMonths(6), 10, Hoy.AddMonths(-9)),
            LoteDe(1, Hoy.AddMonths(7), 10, Hoy.AddMonths(-9))
        };

        var propuesta = PropuestaFefo.Calcular(lotes, 40, Hoy);

        Assert.Equal(new[] { 5, 8, 2, 1 }, propuesta.Asignaciones.Select(a => a.IdLote));
    }

    [Fact]
    public void Ignora_lotes_vencidos_y_lotes_sin_stock()
    {
        var lotes = new[]
        {
            LoteDe(1, Hoy.AddDays(-1), 100),
            LoteDe(2, Hoy, 100),
            LoteDe(3, Hoy.AddMonths(2), 0),
            LoteDe(4, Hoy.AddMonths(8), 15)
        };

        var propuesta = PropuestaFefo.Calcular(lotes, 15, Hoy);

        Assert.Equal(4, Assert.Single(propuesta.Asignaciones).IdLote);
        Assert.Equal(15, propuesta.ExistenciasVigentes);
    }

    [Fact]
    public void Salida_exacta_del_total_vacia_todos_los_lotes_vigentes()
    {
        var lotes = new[] { LoteDe(1, Hoy.AddMonths(3), 10), LoteDe(2, Hoy.AddMonths(9), 5) };

        var propuesta = PropuestaFefo.Calcular(lotes, 15, Hoy);

        Assert.Equal(15, propuesta.Asignaciones.Sum(a => a.Cantidad));
    }

    [Fact]
    public void Cantidad_mayor_a_las_existencias_vigentes_marca_stock_insuficiente()
    {
        var lotes = new[] { LoteDe(1, Hoy.AddMonths(3), 10), LoteDe(2, Hoy.AddDays(-2), 500) };

        var error = Assert.Throws<DomainException>(() => PropuestaFefo.Calcular(lotes, 11, Hoy));

        Assert.StartsWith("Stock insuficiente", error.Message);
        Assert.Contains("10", error.Message);
    }

    [Fact]
    public void Sin_lotes_vigentes_marca_sin_existencias()
    {
        var error = Assert.Throws<DomainException>(() => PropuestaFefo.Calcular([], 1, Hoy));

        Assert.StartsWith("Sin existencias", error.Message);
    }

    [Fact]
    public void Con_solo_lotes_vencidos_marca_sin_existencias()
    {
        var lotes = new[] { LoteDe(1, Hoy.AddDays(-10), 40) };

        var error = Assert.Throws<DomainException>(() => PropuestaFefo.Calcular(lotes, 1, Hoy));

        Assert.StartsWith("Sin existencias", error.Message);
        Assert.Contains("vencidos", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Cantidad_no_positiva_es_rechazada(int cantidad)
    {
        var lotes = new[] { LoteDe(1, Hoy.AddMonths(3), 10) };

        var error = Assert.Throws<DomainException>(() => PropuestaFefo.Calcular(lotes, cantidad, Hoy));

        Assert.Equal("La cantidad debe ser un número entero mayor que 0", error.Message);
    }

    [Fact]
    public void Calcular_no_modifica_el_stock_de_los_lotes()
    {
        var lote = LoteDe(1, Hoy.AddMonths(3), 10);

        PropuestaFefo.Calcular([lote], 4, Hoy);

        Assert.Equal(10, lote.Stock);
    }
}

public class MovimientoSalidaTests
{
    [Fact]
    public void CrearSalida_arma_movimiento_de_salida_con_un_detalle_por_lote()
    {
        var asignaciones = new[]
        {
            new AsignacionLote(2, "L-2", new DateOnly(2027, 1, 1), 30),
            new AsignacionLote(3, "L-3", new DateOnly(2027, 3, 1), 10)
        };

        var movimiento = Movimiento.CrearSalida(7, 4, asignaciones, new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(Movimiento.TipoSalida, movimiento.Tipo);
        Assert.Equal(7, movimiento.IdMedicamento);
        Assert.Equal(4, movimiento.IdUsuario);
        Assert.Equal(40, movimiento.CantidadTotal);
        Assert.Equal(new[] { 30, 10 }, movimiento.Detalles.Select(d => d.Cantidad));
        Assert.Equal(new[] { 2, 3 }, movimiento.Detalles.Select(d => d.IdLote));
    }

    [Fact]
    public void CrearSalida_sin_asignaciones_es_rechazada()
    {
        Assert.Throws<DomainException>(() =>
            Movimiento.CrearSalida(7, 4, [], DateTime.UtcNow));
    }
}
