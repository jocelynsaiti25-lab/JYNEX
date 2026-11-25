using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Domain.Enums;

namespace EtbotFarma.Domain.Tests.Entities;

public class EntradaLoteDomainTests
{
    [Fact]
    public void Medicamento_inactivo_lanza_excepcion_al_validar_para_entrada()
    {
        var medicamentoInactivo = Medicamento.Reconstruir(
            1, "Paracetamol", "Paracetamol", "Tabletas", "500 mg", false, 10, false, DateTime.UtcNow);

        var ex = Assert.Throws<DomainException>(() => medicamentoInactivo.ValidarDisponibleParaEntrada());
        Assert.Contains("inactivo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Lote_entrada_con_cantidad_cero_o_negativa_lanza_excepcion()
    {
        var hoy = new DateOnly(2026, 11, 25);
        var caducidad = new DateOnly(2027, 6, 1);

        Assert.Throws<DomainException>(() => Lote.CrearEntrada(1, "LOT-01", caducidad, 0, hoy));
        Assert.Throws<DomainException>(() => Lote.CrearEntrada(1, "LOT-01", caducidad, -5, hoy));
    }

    [Fact]
    public void Lote_entrada_con_fecha_caducada_o_igual_a_hoy_lanza_excepcion()
    {
        var hoy = new DateOnly(2026, 11, 25);

        Assert.Throws<DomainException>(() => Lote.CrearEntrada(1, "LOT-01", hoy, 10, hoy));
        Assert.Throws<DomainException>(() => Lote.CrearEntrada(1, "LOT-01", hoy.AddDays(-1), 10, hoy));
    }

    [Fact]
    public void Lote_entrada_calcula_semaforo_rojo_amarillo_y_verde_correctamente()
    {
        var hoy = new DateOnly(2026, 11, 25);

        var loteRojo = Lote.CrearEntrada(1, "LOT-R", hoy.AddMonths(2), 15, hoy);
        var loteAmarillo = Lote.CrearEntrada(1, "LOT-A", hoy.AddMonths(6), 15, hoy);
        var loteVerde = Lote.CrearEntrada(1, "LOT-V", hoy.AddMonths(14), 15, hoy);

        Assert.Equal(SemaforoCaducidad.Rojo, loteRojo.ObtenerSemaforo(hoy));
        Assert.Equal(SemaforoCaducidad.Amarillo, loteAmarillo.ObtenerSemaforo(hoy));
        Assert.Equal(SemaforoCaducidad.Verde, loteVerde.ObtenerSemaforo(hoy));
    }

    [Fact]
    public void Movimiento_crear_entrada_lote_genera_tipo_entrada_y_detalle()
    {
        var fecha = new DateTime(2026, 11, 25, 14, 30, 0, DateTimeKind.Utc);
        var mov = Movimiento.CrearEntradaLote(10, 25, 3, 50, fecha);

        Assert.Equal(Movimiento.TipoEntrada, mov.Tipo);
        Assert.Equal(10, mov.IdMedicamento);
        Assert.Equal(3, mov.IdUsuario);
        Assert.Equal(50, mov.CantidadTotal);
        Assert.Single(mov.Detalles);
        Assert.Equal(25, mov.Detalles.First().IdLote);
        Assert.Equal(50, mov.Detalles.First().Cantidad);
    }
}