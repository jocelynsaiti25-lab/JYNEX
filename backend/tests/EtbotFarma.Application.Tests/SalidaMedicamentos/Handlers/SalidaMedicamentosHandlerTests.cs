using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Handlers;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.SalidaMedicamentos.Handlers;

public class SalidaMedicamentosHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 26);

    private readonly MedicamentoRepositoryFalso _medicamentos = new();
    private readonly MovimientoRepositoryFalso _movimientos = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly UsuarioActualFalso _usuario = new() { IdUsuario = 7, Rol = "Empleado" };
    private readonly IDateTimeProvider _reloj = new RelojFalso(new DateTimeOffset(2026, 11, 26, 15, 0, 0, TimeSpan.Zero));

    public SalidaMedicamentosHandlerTests()
    {
        _medicamentos.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Paracetamol", "Paracetamol", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));
        _medicamentos.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Naproxeno", "Naproxeno", "Tabletas", "250 mg", false, 0, false, DateTime.UtcNow));
    }

    private static Lote LoteDePrueba(int id, DateOnly caducidad, int stock) =>
        Lote.Reconstruir(id, 1, $"L{id:000}", caducidad, stock, new DateOnly(2026, 1, 1));

    private static LoteRepositoryFalso LotesEjemplo(bool sinFilas = false)
    {
        var lotes = new LoteRepositoryFalso { SimularDescuentoSinFilas = sinFilas };
        lotes.Lotes.Add(LoteDePrueba(3, new DateOnly(2027, 1, 15), 20));
        lotes.Lotes.Add(LoteDePrueba(1, new DateOnly(2026, 12, 10), 5));
        lotes.Lotes.Add(LoteDePrueba(2, new DateOnly(2026, 12, 20), 10));
        lotes.Lotes.Add(LoteDePrueba(4, Hoy.AddDays(-2), 100));
        return lotes;
    }

    private RegistrarSalidaMedicamentoHandler Registrar(ILoteRepository lotes, IMovimientoRepository? movimientos = null) =>
        new(_medicamentos, lotes, movimientos ?? _movimientos, _usuario, _unitOfWork, _reloj);

    private ProponerSalidaMedicamentoHandler Proponer(ILoteRepository lotes) =>
        new(_medicamentos, lotes, _usuario, _reloj);

    private static int StockRestante(LoteRepositoryFalso lotes, int idLote) =>
        lotes.Lotes.Single(l => l.Id == idLote).Stock
        - lotes.Descuentos.Where(d => d.IdLote == idLote).Sum(d => d.Cantidad);

    [Fact]
    public async Task Registra_la_salida_en_orden_FEFO_descuenta_y_guarda_el_movimiento_con_sus_detalles()
    {
        var lotes = LotesEjemplo();

        var dto = await Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(1, 12), CancellationToken.None);

        Assert.Equal(0, StockRestante(lotes, 1));
        Assert.Equal(3, StockRestante(lotes, 2));
        Assert.Equal(20, StockRestante(lotes, 3));
        Assert.Equal(100, StockRestante(lotes, 4));

        var movimiento = Assert.Single(_movimientos.Movimientos);
        Assert.Equal(Movimiento.TipoSalida, movimiento.Tipo);
        Assert.Equal(7, movimiento.IdUsuario);
        Assert.Equal(12, movimiento.CantidadTotal);
        Assert.Equal(lotes.Descuentos, movimiento.Detalles.Select(d => (d.IdLote, d.Cantidad)));
        Assert.Equal(1, _unitOfWork.VecesGuardado);
        Assert.Equal(12, dto.CantidadRetirada);
        Assert.Equal(new[] { 1, 2 }, dto.LotesUtilizados.Select(l => l.IdLote));
    }

    [Fact]
    public async Task Con_un_solo_lote_suficiente_solo_descuenta_de_el()
    {
        var lotes = LotesEjemplo();

        await Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(1, 4), CancellationToken.None);

        Assert.Equal((1, 4), Assert.Single(lotes.Descuentos));
    }

    [Fact]
    public async Task Con_existencias_insuficientes_no_descuenta_ni_registra_nada()
    {
        var lotes = LotesEjemplo();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(1, 36), CancellationToken.None));

        Assert.Contains("Existencias insuficientes", ex.Message);
        Assert.Empty(lotes.Descuentos);
        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Los_lotes_caducados_no_cuentan_aunque_tengan_existencias()
    {
        var lotes = LotesEjemplo();

        await Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(1, 35), CancellationToken.None);

        Assert.DoesNotContain(lotes.Descuentos, d => d.IdLote == 4);
        Assert.Equal(35, lotes.Descuentos.Sum(d => d.Cantidad));
    }

    [Fact]
    public async Task Sin_lotes_con_existencias_responde_existencias_insuficientes()
    {
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDePrueba(1, new DateOnly(2027, 1, 1), 0));

        await Assert.ThrowsAsync<DomainException>(() =>
            Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(1, 1), CancellationToken.None));
        Assert.Empty(_movimientos.Movimientos);
    }

    [Fact]
    public async Task Sin_sesion_no_hace_nada()
    {
        _usuario.IdUsuario = null;
        var lotes = LotesEjemplo();

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(1, 1), CancellationToken.None));
        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Proponer(lotes).Handle(new ProponerSalidaMedicamentoQuery(1, 1), CancellationToken.None));
        Assert.Empty(lotes.Descuentos);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(2)]
    public async Task Un_medicamento_inexistente_o_inactivo_responde_no_encontrado(int idMedicamento)
    {
        var lotes = LotesEjemplo();

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(idMedicamento, 1), CancellationToken.None));
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Proponer(lotes).Handle(new ProponerSalidaMedicamentoQuery(idMedicamento, 1), CancellationToken.None));
        Assert.Empty(lotes.Descuentos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Si_el_descuento_no_afecta_filas_la_salida_se_cancela_sin_movimiento()
    {
        var lotes = LotesEjemplo(sinFilas: true);

        await Assert.ThrowsAsync<DomainException>(() =>
            Registrar(lotes).Handle(new RegistrarSalidaMedicamentoCommand(1, 3), CancellationToken.None));

        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Si_falla_el_registro_del_movimiento_no_se_confirma_la_transaccion()
    {
        var lotes = LotesEjemplo();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Registrar(lotes, new MovimientoQueFalla()).Handle(new RegistrarSalidaMedicamentoCommand(1, 3), CancellationToken.None));

        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task La_propuesta_reparte_en_FEFO_sin_descontar_ni_guardar()
    {
        var lotes = LotesEjemplo();

        var dto = await Proponer(lotes).Handle(new ProponerSalidaMedicamentoQuery(1, 12), CancellationToken.None);

        Assert.True(dto.PuedeRealizarse);
        Assert.Equal(35, dto.CantidadDisponible);
        Assert.Equal(new[] { (1, 5), (2, 7) }, dto.Lotes.Select(l => (l.IdLote, l.CantidadAUtilizar)));
        Assert.Empty(lotes.Descuentos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task La_propuesta_avisa_cuando_no_alcanza()
    {
        var dto = await Proponer(LotesEjemplo()).Handle(new ProponerSalidaMedicamentoQuery(1, 50), CancellationToken.None);

        Assert.False(dto.PuedeRealizarse);
        Assert.Equal(35, dto.CantidadDisponible);
    }

    private sealed class MovimientoQueFalla : IMovimientoRepository
    {
        public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Falla simulada al guardar el movimiento");
    }
}
