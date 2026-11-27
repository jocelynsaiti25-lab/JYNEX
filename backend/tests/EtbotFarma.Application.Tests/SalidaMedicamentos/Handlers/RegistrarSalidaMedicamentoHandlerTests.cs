using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.SalidaMedicamentos.Commands;
using EtbotFarma.Application.SalidaMedicamentos.Handlers;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.SalidaMedicamentos.Handlers;

public class RegistrarSalidaMedicamentoHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 25);

    private readonly MedicamentoRepositoryFalso _medicamentos = new();
    private readonly MovimientoRepositoryFalso _movimientos = new();
    private readonly UnitOfWorkFalso _unidadDeTrabajo = new();
    private readonly LoteRepositoryFalso _lotes = new();

    private RegistrarSalidaMedicamentoHandler CrearHandler(LoteRepositoryFalso? lotes = null, int? idUsuario = 4)
    {
        var reloj = new RelojFalso(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        return new RegistrarSalidaMedicamentoHandler(
            _medicamentos,
            lotes ?? _lotes,
            _movimientos,
            new UsuarioActualFalso { IdUsuario = idUsuario },
            _unidadDeTrabajo,
            reloj);
    }

    private void Preparar(params (int Id, DateOnly Caducidad, int Stock)[] lotes)
    {
        _medicamentos.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Tempra", "Paracetamol", "Tabletas", "500 mg", false, 0, true, DateTime.UtcNow));
        foreach (var (id, caducidad, stock) in lotes)
            _lotes.Lotes.Add(Lote.Reconstruir(id, 1, $"L-{id}", caducidad, stock, Hoy.AddMonths(-6)));
    }

    [Fact]
    public async Task Registra_salida_descuenta_lotes_en_orden_FEFO_y_guarda_una_sola_vez()
    {
        Preparar((1, Hoy.AddMonths(10), 50), (2, Hoy.AddMonths(4), 30));

        var resultado = await CrearHandler().Handle(new RegistrarSalidaMedicamentoCommand(1, "40"), CancellationToken.None);

        Assert.Equal("Salida registrada", resultado.Mensaje);
        Assert.Equal(40, resultado.CantidadTotal);
        Assert.Equal(new[] { (2, 30), (1, 10) }, _lotes.Descuentos);
        Assert.Equal(0, _lotes.Lotes.Single(l => l.Id == 2).Stock);
        Assert.Equal(40, _lotes.Lotes.Single(l => l.Id == 1).Stock);
        Assert.Equal(1, _unidadDeTrabajo.VecesGuardado);
    }

    [Fact]
    public async Task Guarda_movimiento_de_salida_con_detalle_por_lote_y_usuario_actual()
    {
        Preparar((1, Hoy.AddMonths(10), 50), (2, Hoy.AddMonths(4), 30));

        await CrearHandler(idUsuario: 4).Handle(new RegistrarSalidaMedicamentoCommand(1, "40"), CancellationToken.None);

        var movimiento = Assert.Single(_movimientos.Movimientos);
        Assert.Equal(Movimiento.TipoSalida, movimiento.Tipo);
        Assert.Equal(4, movimiento.IdUsuario);
        Assert.Equal(40, movimiento.CantidadTotal);
        Assert.Equal(2, movimiento.Detalles.Count);
    }

    [Fact]
    public async Task Stock_insuficiente_no_descuenta_ni_guarda_nada()
    {
        Preparar((1, Hoy.AddMonths(4), 10));

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            CrearHandler().Handle(new RegistrarSalidaMedicamentoCommand(1, "11"), CancellationToken.None));

        Assert.StartsWith("Stock insuficiente", error.Message);
        Assert.Empty(_lotes.Descuentos);
        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unidadDeTrabajo.VecesGuardado);
    }

    [Fact]
    public async Task Medicamento_sin_lotes_vigentes_responde_sin_existencias()
    {
        Preparar((1, Hoy.AddDays(-3), 20));

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            CrearHandler().Handle(new RegistrarSalidaMedicamentoCommand(1, "1"), CancellationToken.None));

        Assert.StartsWith("Sin existencias", error.Message);
        Assert.Equal(0, _unidadDeTrabajo.VecesGuardado);
    }

    [Fact]
    public async Task Si_el_descuento_no_afecta_filas_por_concurrencia_no_guarda_la_salida()
    {
        Preparar((1, Hoy.AddMonths(4), 10));
        var lotesConcurrentes = new LoteRepositoryFalso { SimularDescuentoSinFilas = true };
        lotesConcurrentes.Lotes.AddRange(_lotes.Lotes);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            CrearHandler(lotesConcurrentes).Handle(new RegistrarSalidaMedicamentoCommand(1, "5"), CancellationToken.None));

        Assert.StartsWith("Stock insuficiente", error.Message);
        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unidadDeTrabajo.VecesGuardado);
    }

    [Fact]
    public async Task Medicamento_inactivo_no_permite_salida_ni_cambia_el_inventario()
    {
        _medicamentos.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Inactivo", "Sustancia", "Tabletas", "500 mg", false, 0, false, DateTime.UtcNow));
        _lotes.Lotes.Add(Lote.Reconstruir(9, 2, "L-9", Hoy.AddMonths(6), 10, Hoy.AddMonths(-1)));

        await Assert.ThrowsAsync<DomainException>(() =>
            CrearHandler().Handle(new RegistrarSalidaMedicamentoCommand(2, "1"), CancellationToken.None));

        Assert.Empty(_lotes.Descuentos);
        Assert.Equal(10, _lotes.Lotes.Single(l => l.Id == 9).Stock);
        Assert.Empty(_movimientos.Movimientos);
        Assert.Equal(0, _unidadDeTrabajo.VecesGuardado);
    }

    [Fact]
    public async Task Con_misma_caducidad_la_salida_toma_primero_el_lote_de_menor_id()
    {
        Preparar((8, Hoy.AddMonths(4), 10), (5, Hoy.AddMonths(4), 10));

        await CrearHandler().Handle(new RegistrarSalidaMedicamentoCommand(1, "12"), CancellationToken.None);

        Assert.Equal(new[] { (5, 10), (8, 2) }, _lotes.Descuentos);
        Assert.Equal(0, _lotes.Lotes.Single(l => l.Id == 5).Stock);
        Assert.Equal(8, _lotes.Lotes.Single(l => l.Id == 8).Stock);
    }

    [Fact]
    public async Task Medicamento_inexistente_responde_no_encontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            CrearHandler().Handle(new RegistrarSalidaMedicamentoCommand(99, "1"), CancellationToken.None));
    }

    [Fact]
    public async Task Sin_usuario_autenticado_responde_no_autenticado()
    {
        Preparar((1, Hoy.AddMonths(4), 10));

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            CrearHandler(idUsuario: null).Handle(new RegistrarSalidaMedicamentoCommand(1, "1"), CancellationToken.None));
    }
}
