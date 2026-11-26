using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.MedicamentosAgotados.Handlers;
using EtbotFarma.Application.MedicamentosAgotados.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.MedicamentosAgotados.Handlers;

public class ObtenerMedicamentosAgotadosHandlerTests
{
    private static ObtenerMedicamentosAgotadosHandler CrearHandler(
        MedicamentoRepositoryFalso repoMed, LoteRepositoryFalso repoLotes, int? idUsuario = 1)
    {
        var usuarioActual = new UsuarioActualFalso { IdUsuario = idUsuario };
        var reloj = new RelojFalso(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        return new ObtenerMedicamentosAgotadosHandler(repoMed, repoLotes, usuarioActual, reloj);
    }

    [Fact]
    public async Task Solo_lista_medicamentos_sin_existencias_vigentes()
    {
        // HU-12 escenarios 1 y 2: independiente del semáforo, solo importa si hay existencias vigentes
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Advil", "Ibuprofeno", "Cápsulas", "400 mg", false, 0, true, DateTime.UtcNow));
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(3, "Amoxilina", "Amoxicilina", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));

        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-1", hoy.AddMonths(6), 10, hoy)); // medicamento 1: con existencias (no agotado)
        // medicamento 2: sin ningún lote → agotado
        repoLotes.Lotes.Add(Lote.Reconstruir(2, 3, "L-3", hoy.AddDays(-5), 20, hoy.AddMonths(-6))); // medicamento 3: solo lote caducado → agotado

        var handler = CrearHandler(repoMed, repoLotes);
        var resultados = await handler.Handle(new ObtenerMedicamentosAgotadosQuery(), CancellationToken.None);

        Assert.Equal(2, resultados.Count);
        Assert.Contains(resultados, r => r.IdMedicamento == 2);
        Assert.Contains(resultados, r => r.IdMedicamento == 3);
        Assert.DoesNotContain(resultados, r => r.IdMedicamento == 1);
    }

    [Fact]
    public async Task Devuelve_los_datos_descriptivos_del_medicamento_agotado()
    {
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));

        var handler = CrearHandler(repoMed, new LoteRepositoryFalso());
        var resultados = await handler.Handle(new ObtenerMedicamentosAgotadosQuery(), CancellationToken.None);

        var agotado = Assert.Single(resultados);
        Assert.Equal("Tempra", agotado.NombreComercial);
        Assert.Equal("Paracetamol", agotado.NombreSustancia);
        Assert.Equal("Tabletas", agotado.Presentacion);
        Assert.Equal("500 mg", agotado.Concentracion);
    }

    [Fact]
    public async Task Sin_medicamentos_agotados_devuelve_lista_vacia()
    {
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));

        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "L-1", hoy.AddMonths(6), 10, hoy));

        var handler = CrearHandler(repoMed, repoLotes);
        var resultados = await handler.Handle(new ObtenerMedicamentosAgotadosQuery(), CancellationToken.None);

        Assert.Empty(resultados);
    }

    [Fact]
    public async Task Sin_sesion_activa_lanza_NoAutenticadoException()
    {
        var handler = CrearHandler(new MedicamentoRepositoryFalso(), new LoteRepositoryFalso(), idUsuario: null);

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            handler.Handle(new ObtenerMedicamentosAgotadosQuery(), CancellationToken.None));
    }
}
