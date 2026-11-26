using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.StockMinimo.Handlers;
using EtbotFarma.Application.StockMinimo.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.StockMinimo.Handlers;

public class ObtenerMedicamentosParaStockMinimoHandlerTests
{
    private static ObtenerMedicamentosParaStockMinimoHandler CrearHandler(
        MedicamentoRepositoryFalso repoMed, LoteRepositoryFalso repoLotes, int? idUsuario = 1, DateTimeOffset? ahora = null)
    {
        var usuarioActual = new UsuarioActualFalso { IdUsuario = idUsuario };
        var reloj = new RelojFalso(ahora ?? new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        return new ObtenerMedicamentosParaStockMinimoHandler(repoMed, repoLotes, usuarioActual, reloj);
    }

    [Fact]
    public async Task Calcula_existencias_vigentes_y_alertas_por_medicamento()
    {
        // HU-11 escenarios 2, 3 y 4
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 10, true, DateTime.UtcNow));
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Advil", "Ibuprofeno", "Cápsulas", "400 mg", false, 5, true, DateTime.UtcNow));
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(3, "Amoxilina", "Amoxicilina", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));

        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-1", hoy.AddMonths(6), 8, hoy)); // medicamento 1: 8 <= 10 → bajo
        repoLotes.Lotes.Add(Lote.Reconstruir(2, 2, "L-2", hoy.AddMonths(6), 20, hoy)); // medicamento 2: 20 > 5 → ok
        // medicamento 3: sin lotes → agotado

        var handler = CrearHandler(repoMed, repoLotes);
        var resultados = await handler.Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);

        var tempra = resultados.Single(r => r.IdMedicamento == 1);
        Assert.Equal(8, tempra.ExistenciasVigentes);
        Assert.True(tempra.EnStockBajo);
        Assert.False(tempra.EstaAgotado);

        var advil = resultados.Single(r => r.IdMedicamento == 2);
        Assert.Equal(20, advil.ExistenciasVigentes);
        Assert.False(advil.EnStockBajo);

        var amoxilina = resultados.Single(r => r.IdMedicamento == 3);
        Assert.Equal(0, amoxilina.ExistenciasVigentes);
        Assert.True(amoxilina.EstaAgotado);
    }

    [Fact]
    public async Task Un_lote_caducado_no_cuenta_como_existencia_vigente()
    {
        // HU-11 "existencias vigentes (no caducadas)"
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 5, true, DateTime.UtcNow));

        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-VENCIDO", hoy.AddDays(-5), 50, hoy.AddMonths(-6)));

        var handler = CrearHandler(repoMed, repoLotes);
        var resultados = await handler.Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);

        var tempra = Assert.Single(resultados);
        Assert.Equal(0, tempra.ExistenciasVigentes);
        Assert.True(tempra.EstaAgotado);
    }

    [Fact]
    public async Task Sin_sesion_activa_lanza_NoAutenticadoException()
    {
        var handler = CrearHandler(new MedicamentoRepositoryFalso(), new LoteRepositoryFalso(), idUsuario: null);

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            handler.Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Un_medicamento_con_todos_los_lotes_caducados_sigue_avisando_que_tiene_lotes_caducados()
    {
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 5, true, DateTime.UtcNow));
        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-VENCIDO", hoy.AddDays(-5), 50, hoy.AddMonths(-6)));

        var resultados = await CrearHandler(repoMed, repoLotes)
            .Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);

        var tempra = Assert.Single(resultados);
        Assert.Equal(0, tempra.ExistenciasVigentes);
        Assert.Equal(50, tempra.ExistenciasCaducadas);
        Assert.True(tempra.TieneLotesCaducados);
        Assert.True(tempra.EstaAgotado);
    }

    [Fact]
    public async Task Con_un_lote_caducado_y_otro_vigente_solo_cuenta_el_vigente_y_se_conserva_el_aviso()
    {
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 5, true, DateTime.UtcNow));
        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-VENCIDO", hoy.AddDays(-5), 50, hoy.AddMonths(-6)));
        repoLotes.Lotes.Add(Lote.Reconstruir(2, 1, "L-VIGENTE", hoy.AddMonths(6), 5, hoy));

        var resultados = await CrearHandler(repoMed, repoLotes)
            .Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);

        var tempra = Assert.Single(resultados);
        Assert.Equal(5, tempra.ExistenciasVigentes);
        Assert.Equal(50, tempra.ExistenciasCaducadas);
        Assert.True(tempra.TieneLotesCaducados);
        Assert.True(tempra.EnStockBajo);
        Assert.False(tempra.EstaAgotado);
    }

    [Theory]
    [InlineData(11, 10, 11)]
    [InlineData(10, 10, 10)]
    [InlineData(9, 10, 9)]
    public async Task Compara_las_existencias_vigentes_con_el_stock_minimo(int existencias, int minimo, int esperadas)
    {
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, minimo, true, DateTime.UtcNow));
        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-1", hoy.AddMonths(6), existencias, hoy));

        var resultados = await CrearHandler(repoMed, repoLotes)
            .Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);

        var tempra = Assert.Single(resultados);
        Assert.Equal(esperadas, tempra.ExistenciasVigentes);
        Assert.Equal(existencias <= minimo, tempra.EnStockBajo);
    }

    [Fact]
    public async Task El_lote_que_caduca_hoy_es_caducado_justo_antes_de_medianoche()
    {
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 5, true, DateTime.UtcNow));
        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-HOY", new DateOnly(2026, 11, 25), 30, new DateOnly(2026, 1, 1)));

        var antesDeMedianoche = new DateTimeOffset(2026, 11, 25, 23, 59, 59, TimeSpan.Zero);
        var resultados = await CrearHandler(repoMed, repoLotes, ahora: antesDeMedianoche)
            .Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);

        var tempra = Assert.Single(resultados);
        Assert.Equal(0, tempra.ExistenciasVigentes);
        Assert.Equal(30, tempra.ExistenciasCaducadas);
    }

    [Fact]
    public async Task El_lote_que_caduca_manana_sigue_vigente_hasta_el_cambio_de_dia()
    {
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 5, true, DateTime.UtcNow));
        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-MANANA", new DateOnly(2026, 11, 26), 30, new DateOnly(2026, 1, 1)));

        var ultimoInstanteDelDia = new DateTimeOffset(2026, 11, 25, 23, 59, 59, TimeSpan.Zero);
        var primerInstanteDelDiaSiguiente = new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);

        var antes = await CrearHandler(repoMed, repoLotes, ahora: ultimoInstanteDelDia)
            .Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);
        var despues = await CrearHandler(repoMed, repoLotes, ahora: primerInstanteDelDiaSiguiente)
            .Handle(new ObtenerMedicamentosParaStockMinimoQuery(), CancellationToken.None);

        Assert.Equal(30, Assert.Single(antes).ExistenciasVigentes);
        Assert.Equal(0, Assert.Single(despues).ExistenciasVigentes);
    }
}
