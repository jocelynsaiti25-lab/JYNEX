using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.StockMinimo.Handlers;
using EtbotFarma.Application.StockMinimo.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.StockMinimo.Handlers;

public class ObtenerMedicamentosParaStockMinimoHandlerTests
{
    private static ObtenerMedicamentosParaStockMinimoHandler CrearHandler(
        MedicamentoRepositoryFalso repoMed, LoteRepositoryFalso repoLotes, int? idUsuario = 1)
    {
        var usuarioActual = new UsuarioActualFalso { IdUsuario = idUsuario };
        var reloj = new RelojFalso(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
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
}
