using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.SalidaMedicamentos.Handlers;
using EtbotFarma.Application.SalidaMedicamentos.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.SalidaMedicamentos.Handlers;

public class ProponerYBuscarSalidaHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 25);

    private readonly MedicamentoRepositoryFalso _medicamentos = new();
    private readonly LoteRepositoryFalso _lotes = new();

    private static RelojFalso Reloj() => new(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));

    private ProponerSalidaMedicamentoHandler Proponer(int? idUsuario = 4)
        => new(_medicamentos, _lotes, new UsuarioActualFalso { IdUsuario = idUsuario }, Reloj());

    private BuscarMedicamentosParaSalidaHandler Buscar(int? idUsuario = 4)
        => new(_medicamentos, _lotes, new UsuarioActualFalso { IdUsuario = idUsuario }, Reloj());

    private void AgregarMedicamento(int id, string nombre, bool controlado = false, bool activo = true)
        => _medicamentos.Medicamentos.Add(Medicamento.Reconstruir(
            id, nombre, "Sustancia " + nombre, "Tabletas", "500 mg", controlado, 0, activo, DateTime.UtcNow));

    private void LoteDe(int id, int idMedicamento, DateOnly caducidad, int stock)
        => _lotes.Lotes.Add(Lote.Reconstruir(id, idMedicamento, $"L-{id}", caducidad, stock, Hoy.AddMonths(-6)));

    [Fact]
    public async Task La_propuesta_muestra_los_lotes_FEFO_sin_modificar_stock()
    {
        AgregarMedicamento(1, "Tempra");
        LoteDe(1, 1, Hoy.AddMonths(10), 50);
        LoteDe(2, 1, Hoy.AddMonths(4), 30);

        var propuesta = await Proponer().Handle(new ProponerSalidaMedicamentoQuery(1, "40"), CancellationToken.None);

        Assert.Equal(new[] { 2, 1 }, propuesta.Lotes.Select(l => l.IdLote));
        Assert.Equal(new[] { 30, 10 }, propuesta.Lotes.Select(l => l.Cantidad));
        Assert.Equal(80, propuesta.ExistenciasVigentes);
        Assert.Empty(_lotes.Descuentos);
        Assert.Equal(30, _lotes.Lotes.Single(l => l.Id == 2).Stock);
    }

    [Fact]
    public async Task La_propuesta_con_cantidad_excesiva_marca_stock_insuficiente()
    {
        AgregarMedicamento(1, "Tempra");
        LoteDe(1, 1, Hoy.AddMonths(4), 10);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            Proponer().Handle(new ProponerSalidaMedicamentoQuery(1, "50"), CancellationToken.None));

        Assert.StartsWith("Stock insuficiente", error.Message);
    }

    [Fact]
    public async Task La_propuesta_de_medicamento_inexistente_responde_no_encontrado()
    {
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            Proponer().Handle(new ProponerSalidaMedicamentoQuery(9, "1"), CancellationToken.None));
    }

    [Fact]
    public async Task La_propuesta_sin_sesion_responde_no_autenticado()
    {
        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Proponer(null).Handle(new ProponerSalidaMedicamentoQuery(1, "1"), CancellationToken.None));
    }

    [Fact]
    public async Task La_busqueda_filtra_por_nombre_o_sustancia_sin_distinguir_mayusculas()
    {
        AgregarMedicamento(1, "Tempra");
        AgregarMedicamento(2, "Aspirina");
        LoteDe(1, 1, Hoy.AddMonths(4), 10);
        LoteDe(2, 2, Hoy.AddMonths(4), 10);

        var porNombre = await Buscar().Handle(new BuscarMedicamentosParaSalidaQuery("temp"), CancellationToken.None);
        var porSustancia = await Buscar().Handle(new BuscarMedicamentosParaSalidaQuery("SUSTANCIA ASPI"), CancellationToken.None);

        Assert.Equal(1, Assert.Single(porNombre).IdMedicamento);
        Assert.Equal(2, Assert.Single(porSustancia).IdMedicamento);
    }

    [Fact]
    public async Task La_busqueda_solo_lista_lotes_vigentes_con_stock_y_marca_agotado()
    {
        AgregarMedicamento(1, "Tempra");
        AgregarMedicamento(2, "Aspirina");
        LoteDe(1, 1, Hoy.AddMonths(10), 50);
        LoteDe(2, 1, Hoy.AddDays(-4), 99);
        LoteDe(3, 1, Hoy.AddMonths(2), 0);
        LoteDe(4, 2, Hoy.AddDays(-4), 20);

        var resultado = await Buscar().Handle(new BuscarMedicamentosParaSalidaQuery(null), CancellationToken.None);

        var tempra = resultado.Single(r => r.IdMedicamento == 1);
        Assert.Equal(50, tempra.ExistenciasVigentes);
        Assert.False(tempra.EstaAgotado);
        Assert.Equal(1, Assert.Single(tempra.Lotes).IdLote);

        var aspirina = resultado.Single(r => r.IdMedicamento == 2);
        Assert.True(aspirina.EstaAgotado);
        Assert.Empty(aspirina.Lotes);
    }

    [Fact]
    public async Task La_busqueda_omite_medicamentos_inactivos_e_indica_si_es_controlado()
    {
        AgregarMedicamento(1, "Tempra", controlado: true);
        AgregarMedicamento(2, "Aspirina", activo: false);

        var resultado = await Buscar().Handle(new BuscarMedicamentosParaSalidaQuery(null), CancellationToken.None);

        var unico = Assert.Single(resultado);
        Assert.Equal(1, unico.IdMedicamento);
        Assert.True(unico.EsControlado);
    }

    [Fact]
    public async Task La_busqueda_sin_sesion_responde_no_autenticado()
    {
        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            Buscar(null).Handle(new BuscarMedicamentosParaSalidaQuery(null), CancellationToken.None));
    }
}
