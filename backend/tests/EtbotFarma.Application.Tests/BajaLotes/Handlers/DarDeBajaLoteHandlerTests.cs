using EtbotFarma.Application.BajaLotes.Commands;
using EtbotFarma.Application.BajaLotes.Handlers;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.BajaLotes.Handlers;

public class DarDeBajaLoteHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 26);

    private readonly MedicamentoRepositoryFalso _medicamentos = new();
    private readonly MovimientoRepositoryFalso _movimientos = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly UsuarioActualFalso _usuario = new() { IdUsuario = 7, Rol = "Empleado" };
    private readonly IDateTimeProvider _reloj = new RelojFalso(new DateTimeOffset(2026, 11, 26, 15, 30, 0, TimeSpan.Zero));

    public DarDeBajaLoteHandlerTests()
    {
        _medicamentos.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Paracetamol", "Paracetamol", "Tabletas", "500 mg", false, 10, true, DateTime.UtcNow));
    }

    private static LoteRepositoryFalso LotesCon(params Lote[] lotes)
    {
        var repositorio = new LoteRepositoryFalso();
        repositorio.Lotes.AddRange(lotes);
        return repositorio;
    }

    private static Lote LoteDePrueba(int id, DateOnly caducidad, int stock) =>
        Lote.Reconstruir(id, 1, $"L-{id}", caducidad, stock, new DateOnly(2026, 1, 10));

    private DarDeBajaLoteHandler Crear(LoteRepositoryFalso lotes) =>
        new(lotes, _medicamentos, _movimientos, _usuario, _unitOfWork, _reloj);

    [Fact]
    public async Task Da_de_baja_un_lote_rojo_registra_la_baja_y_guarda_una_sola_vez()
    {
        var lotes = LotesCon(LoteDePrueba(5, Hoy.AddMonths(2), 40));

        var dto = await Crear(lotes).Handle(new DarDeBajaLoteCommand(5, 40, " Próximo a caducar "), CancellationToken.None);

        Assert.Equal("Lote dado de baja", dto.Mensaje);
        Assert.Equal(40, dto.CantidadRetirada);
        Assert.Equal("Próximo a caducar", dto.Motivo);
        Assert.Equal("L-5", dto.NumeroLote);
        Assert.Contains("Paracetamol", dto.NombreMedicamento);
        Assert.Equal((5, 40), Assert.Single(lotes.Descuentos));
        Assert.Equal(0, lotes.Lotes.Single(l => l.Id == 5).Stock);

        var movimiento = Assert.Single(_movimientos.Movimientos);
        Assert.Equal(Movimiento.TipoBaja, movimiento.Tipo);
        Assert.Equal(7, movimiento.IdUsuario);
        Assert.Equal(40, movimiento.CantidadTotal);
        Assert.Equal(new DateTime(2026, 11, 26, 15, 30, 0, DateTimeKind.Utc), movimiento.Fecha);
        Assert.Equal(1, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Un_lote_caducado_con_existencias_tambien_se_da_de_baja()
    {
        var lotes = LotesCon(LoteDePrueba(5, Hoy.AddDays(-3), 12));

        var dto = await Crear(lotes).Handle(new DarDeBajaLoteCommand(5, 12, "Caducado"), CancellationToken.None);

        Assert.Equal(12, dto.CantidadRetirada);
        Assert.Single(_movimientos.Movimientos);
    }

    [Fact]
    public async Task Solo_se_retira_el_lote_indicado_y_los_demas_conservan_sus_existencias()
    {
        var verde = LoteDePrueba(6, Hoy.AddMonths(24), 30);
        var lotes = LotesCon(LoteDePrueba(5, Hoy.AddMonths(1), 40), verde);

        await Crear(lotes).Handle(new DarDeBajaLoteCommand(5, 40, "Próximo a caducar"), CancellationToken.None);

        Assert.Equal((5, 40), Assert.Single(lotes.Descuentos));
        Assert.Equal(0, lotes.Lotes.Single(l => l.Id == 5).Stock);
        Assert.Equal(30, lotes.Lotes.Single(l => l.Id == 6).Stock);
    }

    [Fact]
    public async Task Sin_sesion_no_hace_nada()
    {
        _usuario.IdUsuario = null;
        var lotes = LotesCon(LoteDePrueba(5, Hoy.AddMonths(1), 40));

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Crear(lotes).Handle(new DarDeBajaLoteCommand(5, 40, "Caducado"), CancellationToken.None));

        Assert.Empty(lotes.Descuentos);
        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Un_lote_inexistente_responde_no_encontrado()
    {
        var lotes = LotesCon();

        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Crear(lotes).Handle(new DarDeBajaLoteCommand(999, 40, "Caducado"), CancellationToken.None));

        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Theory]
    [InlineData(4, 40, 40)]
    [InlineData(24, 40, 40)]
    [InlineData(1, 40, 35)]
    [InlineData(1, 0, 1)]
    public async Task Un_lote_que_no_es_rojo_o_cambio_de_existencias_ya_no_es_elegible(int meses, int stock, int cantidadConsultada)
    {
        var lotes = LotesCon(LoteDePrueba(5, Hoy.AddMonths(meses), stock));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Crear(lotes).Handle(new DarDeBajaLoteCommand(5, cantidadConsultada, "Caducado"), CancellationToken.None));

        Assert.Equal("El lote ya no es elegible.", ex.Message);
        Assert.Empty(lotes.Descuentos);
        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Si_otra_baja_gano_la_carrera_y_el_descuento_no_afecta_filas_no_se_registra_nada()
    {
        var lotes = new LoteRepositoryFalso { SimularDescuentoSinFilas = true };
        lotes.Lotes.Add(LoteDePrueba(5, Hoy.AddMonths(1), 40));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Crear(lotes).Handle(new DarDeBajaLoteCommand(5, 40, "Caducado"), CancellationToken.None));

        Assert.Equal("El lote ya no es elegible.", ex.Message);
        Assert.Empty(lotes.Descuentos);
        Assert.Equal(40, lotes.Lotes.Single(l => l.Id == 5).Stock);
        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    [Fact]
    public async Task Un_lote_ya_dado_de_baja_no_se_puede_dar_de_baja_otra_vez()
    {
        var lotes = LotesCon(LoteDePrueba(5, Hoy.AddMonths(1), 40));
        var handler = Crear(lotes);
        await handler.Handle(new DarDeBajaLoteCommand(5, 40, "Caducado"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DarDeBajaLoteCommand(5, 40, "Caducado"), CancellationToken.None));

        Assert.Equal("El lote ya no es elegible.", ex.Message);
        Assert.Single(lotes.Descuentos);
        Assert.Single(_movimientos.Movimientos);
        Assert.Equal(0, lotes.Lotes.Single(l => l.Id == 5).Stock);
    }
}
