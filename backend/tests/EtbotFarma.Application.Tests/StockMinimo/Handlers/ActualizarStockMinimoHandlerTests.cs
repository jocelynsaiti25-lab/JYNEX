using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.StockMinimo.Commands;
using EtbotFarma.Application.StockMinimo.Handlers;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.StockMinimo.Handlers;

public class ActualizarStockMinimoHandlerTests
{
    [Fact]
    public async Task Guarda_el_nuevo_stock_minimo_y_confirma_el_cambio()
    {
        // HU-11 escenarios 1 y 6
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));
        var unitOfWork = new UnitOfWorkFalso();
        var handler = new ActualizarStockMinimoHandler(repoMed, unitOfWork);

        var resultado = await handler.Handle(new ActualizarStockMinimoCommand(1, 15), CancellationToken.None);

        Assert.Equal(15, resultado.StockMinimo);
        Assert.Equal("Stock mínimo actualizado", resultado.Mensaje);
        Assert.Equal(15, repoMed.Medicamentos[0].StockMinimo);
        Assert.Equal(1, unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Medicamento_inexistente_lanza_NoEncontradoException()
    {
        var repoMed = new MedicamentoRepositoryFalso();
        var handler = new ActualizarStockMinimoHandler(repoMed, new UnitOfWorkFalso());

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            handler.Handle(new ActualizarStockMinimoCommand(99, 10), CancellationToken.None));
    }

    [Fact]
    public async Task Valor_negativo_lanza_DomainException_y_no_guarda_nada()
    {
        // Defensa adicional del Domain (el validator ya lo rechaza en 400 antes de llegar aquí)
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 5, true, DateTime.UtcNow));
        var unitOfWork = new UnitOfWorkFalso();
        var handler = new ActualizarStockMinimoHandler(repoMed, unitOfWork);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ActualizarStockMinimoCommand(1, -1), CancellationToken.None));

        Assert.Equal(5, repoMed.Medicamentos[0].StockMinimo);
        Assert.Equal(0, unitOfWork.VecesGuardado);
    }
}
