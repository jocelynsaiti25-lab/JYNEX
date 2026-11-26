using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Domain.Enums;

namespace EtbotFarma.Domain.Tests.Entities;

public sealed class MedicamentoYLoteTests
{
    [Fact]
    public void Crear_medicamento_con_datos_validos_inicia_activo_y_con_stock_minimo_cero()
    {
        var ahora = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

        var medicamento = Medicamento.Crear(
            "Tempra",
            "Paracetamol",
            "Tabletas",
            "500 mg",
            esControlado: true,
            ahora);

        Assert.Equal("Tempra", medicamento.NombreComercial);
        Assert.True(medicamento.EsControlado);
        Assert.Equal(0, medicamento.StockMinimo);
        Assert.True(medicamento.Activo);
    }

    [Fact]
    public void Lote_con_fecha_caducada_hoy_o_anterior_lanza_excepcion()
    {
        var hoy = new DateOnly(2026, 5, 10);

        var exHoy = Assert.Throws<DomainException>(() =>
            Lote.Crear(1, "LOTE-01", hoy, 10, hoy));
        Assert.Equal("No se puede registrar un lote caducado.", exHoy.Message);

        var exAyer = Assert.Throws<DomainException>(() =>
            Lote.Crear(1, "LOTE-01", hoy.AddDays(-1), 10, hoy));
        Assert.Equal("No se puede registrar un lote caducado.", exAyer.Message);
    }

    [Fact]
    public void Lote_con_caracteres_invalidos_o_mas_de_50_lanza_excepcion()
    {
        var hoy = new DateOnly(2026, 5, 10);

        Assert.Throws<DomainException>(() =>
            Lote.Crear(1, "LOTE 01", hoy.AddMonths(6), 10, hoy));

        Assert.Throws<DomainException>(() =>
            Lote.Crear(1, "LOTE@01", hoy.AddMonths(6), 10, hoy));

        Assert.Throws<DomainException>(() =>
            Lote.Crear(1, new string('A', 51), hoy.AddMonths(6), 10, hoy));
    }

    [Fact]
    public void Lote_con_cantidad_negativa_lanza_excepcion()
    {
        var hoy = new DateOnly(2026, 5, 10);

        var ex = Assert.Throws<DomainException>(() =>
            Lote.Crear(1, "LOTE-01", hoy.AddMonths(6), -1, hoy));

        Assert.Equal("La cantidad debe ser un número entero de 0 o más.", ex.Message);
    }

    [Fact]
    public void Semaforo_calcula_rojo_amarillo_y_verde_por_meses_calendario()
    {
        var hoy = new DateOnly(2026, 5, 10);

        var loteRojo = Lote.Crear(1, "L-ROJO", hoy.AddMonths(3), 5, hoy);
        var loteAmarillo = Lote.Crear(1, "L-AMARILLO", hoy.AddMonths(6), 5, hoy);
        var loteVerde = Lote.Crear(1, "L-VERDE", hoy.AddMonths(13), 5, hoy);

        Assert.Equal(SemaforoCaducidad.Rojo, loteRojo.ObtenerSemaforo(hoy));
        Assert.Equal(SemaforoCaducidad.Amarillo, loteAmarillo.ObtenerSemaforo(hoy));
        Assert.Equal(SemaforoCaducidad.Verde, loteVerde.ObtenerSemaforo(hoy));
    }

    [Fact]
    public void Semaforo_en_limites_exactos_de_tres_y_doce_meses_no_deja_fechas_sin_clasificar()
    {
        // HU-10 escenario 6: el límite exacto de 3 meses es Rojo y el de 12 meses es Amarillo
        var hoy = new DateOnly(2026, 5, 10);

        var loteLimiteRojo = Lote.Crear(1, "L-LIMITE-ROJO", hoy.AddMonths(3), 5, hoy);
        var loteLimiteAmarillo = Lote.Crear(1, "L-LIMITE-AMARILLO", hoy.AddMonths(12), 5, hoy);

        Assert.Equal(SemaforoCaducidad.Rojo, loteLimiteRojo.ObtenerSemaforo(hoy));
        Assert.Equal(SemaforoCaducidad.Amarillo, loteLimiteAmarillo.ObtenerSemaforo(hoy));
    }

    [Fact]
    public void Lote_vencido_esta_caducado_y_se_clasifica_en_rojo()
    {
        // HU-10 escenario 5: un lote vencido (hoy o antes) se marca como caducado y es Rojo
        var hoy = new DateOnly(2026, 5, 10);

        var loteVencidoAyer = Lote.Reconstruir(1, 1, "L-VENCIDO", hoy.AddDays(-1), 5, hoy.AddMonths(-6));
        var loteVenceHoy = Lote.Reconstruir(2, 1, "L-VENCE-HOY", hoy, 5, hoy.AddMonths(-6));
        var loteVigente = Lote.Reconstruir(3, 1, "L-VIGENTE", hoy.AddMonths(6), 5, hoy.AddMonths(-1));

        Assert.True(loteVencidoAyer.EstaCaducado(hoy));
        Assert.Equal(SemaforoCaducidad.Rojo, loteVencidoAyer.ObtenerSemaforo(hoy));

        Assert.True(loteVenceHoy.EstaCaducado(hoy));
        Assert.Equal(SemaforoCaducidad.Rojo, loteVenceHoy.ObtenerSemaforo(hoy));

        Assert.False(loteVigente.EstaCaducado(hoy));
    }
}