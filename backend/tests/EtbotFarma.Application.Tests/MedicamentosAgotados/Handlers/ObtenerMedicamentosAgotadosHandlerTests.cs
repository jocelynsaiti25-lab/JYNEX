using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.MedicamentosAgotados.Dtos;
using EtbotFarma.Application.MedicamentosAgotados.Handlers;
using EtbotFarma.Application.MedicamentosAgotados.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.MedicamentosAgotados.Handlers;

public class ObtenerMedicamentosAgotadosHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 25);

    private static ObtenerMedicamentosAgotadosHandler CrearHandler(
        MedicamentoRepositoryFalso medicamentos, LoteRepositoryFalso lotes, int? idUsuario = 1)
    {
        var usuarioActual = new UsuarioActualFalso { IdUsuario = idUsuario };
        var reloj = new RelojFalso(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        return new ObtenerMedicamentosAgotadosHandler(medicamentos, lotes, usuarioActual, reloj);
    }

    private static Medicamento Med(int id, string nombre, string presentacion = "Tabletas", string concentracion = "500 mg", int stockMinimo = 0, bool activo = true)
        => Medicamento.Reconstruir(id, nombre, "Sustancia " + nombre, presentacion, concentracion, false, stockMinimo, activo, DateTime.UtcNow);

    private static Lote LoteDe(int id, int idMedicamento, DateOnly caducidad, int stock)
        => Lote.Reconstruir(id, idMedicamento, $"L-{id}", caducidad, stock, Hoy.AddMonths(-6));

    private static async Task<IReadOnlyList<MedicamentoAgotadoDto>> ConsultarAsync(
        MedicamentoRepositoryFalso medicamentos, LoteRepositoryFalso lotes, string? termino = null)
        => await CrearHandler(medicamentos, lotes).Handle(new ObtenerMedicamentosAgotadosQuery(termino), CancellationToken.None);

    [Fact]
    public async Task Medicamento_con_todos_sus_lotes_en_cero_aparece_con_la_etiqueta_SIN_EXISTENCIAS()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddMonths(8), 0));
        lotes.Lotes.Add(LoteDe(2, 1, Hoy.AddMonths(14), 0));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, lotes));

        Assert.Equal(1, resultado.IdMedicamento);
        Assert.Equal("SIN EXISTENCIAS", resultado.Etiqueta);
    }

    [Fact]
    public async Task Medicamento_con_solo_lotes_caducados_con_unidades_aparece_como_agotado()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddDays(-5), 40));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, lotes));

        Assert.Equal(1, resultado.IdMedicamento);
    }

    [Fact]
    public async Task Medicamento_sin_lotes_aparece_como_agotado()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, new LoteRepositoryFalso()));

        Assert.Equal("SIN EXISTENCIAS", resultado.Etiqueta);
    }

    [Fact]
    public async Task Medicamento_con_un_lote_vigente_con_unidades_no_aparece()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddMonths(8), 5));

        Assert.Empty(await ConsultarAsync(medicamentos, lotes));
    }

    [Fact]
    public async Task Un_lote_rojo_que_todavia_no_caduca_cuenta_como_existencia()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddMonths(2), 12));

        Assert.Empty(await ConsultarAsync(medicamentos, lotes));
    }

    [Fact]
    public async Task Un_lote_caducado_con_unidades_no_compensa_la_falta_de_vigentes_pero_uno_vigente_si_lo_saca_de_la_lista()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        medicamentos.Medicamentos.Add(Med(2, "Advil"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddDays(-1), 30));
        lotes.Lotes.Add(LoteDe(2, 1, Hoy.AddMonths(10), 4));
        lotes.Lotes.Add(LoteDe(3, 2, Hoy.AddDays(-1), 30));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, lotes));

        Assert.Equal(2, resultado.IdMedicamento);
    }

    [Fact]
    public async Task El_lote_que_caduca_hoy_ya_no_cuenta_y_el_que_caduca_manana_si()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        medicamentos.Medicamentos.Add(Med(2, "Advil"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy, 10));
        lotes.Lotes.Add(LoteDe(2, 2, Hoy.AddDays(1), 10));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, lotes));

        Assert.Equal(1, resultado.IdMedicamento);
    }

    [Fact]
    public async Task Cada_combinacion_de_nombre_presentacion_y_concentracion_es_un_medicamento_distinto()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Paracetamol", "Tabletas", "500 mg"));
        medicamentos.Medicamentos.Add(Med(2, "Paracetamol", "Jarabe", "120 mg/5 ml"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 2, Hoy.AddMonths(9), 20));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, lotes));

        Assert.Equal(1, resultado.IdMedicamento);
        Assert.Equal("Tabletas", resultado.Presentacion);
        Assert.Equal("500 mg", resultado.Concentracion);
    }

    [Fact]
    public async Task Cada_fila_muestra_nombre_presentacion_y_concentracion()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(7, "Amoxilina", "Cápsulas", "250 mg"));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, new LoteRepositoryFalso()));

        Assert.Equal("Amoxilina", resultado.NombreComercial);
        Assert.Equal("Sustancia Amoxilina", resultado.NombreSustancia);
        Assert.Equal("Cápsulas", resultado.Presentacion);
        Assert.Equal("250 mg", resultado.Concentracion);
    }

    [Fact]
    public async Task Con_stock_minimo_y_cero_vigentes_el_medicamento_figura_en_agotados()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra", stockMinimo: 10));

        var resultado = Assert.Single(await ConsultarAsync(medicamentos, new LoteRepositoryFalso()));

        Assert.Equal(1, resultado.IdMedicamento);
    }

    [Fact]
    public async Task Una_entrada_con_cero_unidades_no_lo_saca_de_agotados_y_una_con_unidades_si()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddMonths(12), 0));

        Assert.Single(await ConsultarAsync(medicamentos, lotes));

        lotes.Lotes.Add(LoteDe(2, 1, Hoy.AddMonths(12), 25));

        Assert.Empty(await ConsultarAsync(medicamentos, lotes));
    }

    [Fact]
    public async Task Si_todos_tienen_existencias_vigentes_la_lista_queda_vacia()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        medicamentos.Medicamentos.Add(Med(2, "Advil"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddMonths(9), 3));
        lotes.Lotes.Add(LoteDe(2, 2, Hoy.AddMonths(9), 3));

        Assert.Empty(await ConsultarAsync(medicamentos, lotes));
    }

    [Fact]
    public async Task Los_medicamentos_inactivos_no_aparecen_en_agotados()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra", activo: false));

        Assert.Empty(await ConsultarAsync(medicamentos, new LoteRepositoryFalso()));
    }

    [Theory]
    [InlineData("tem", 1)]
    [InlineData("  TEMPRA  ", 1)]
    [InlineData("sustancia advil", 1)]
    [InlineData("zzz", 0)]
    [InlineData("", 2)]
    [InlineData(null, 2)]
    public async Task La_busqueda_filtra_solo_entre_los_agotados_por_nombre_o_sustancia(string? termino, int esperados)
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        medicamentos.Medicamentos.Add(Med(2, "Advil"));
        medicamentos.Medicamentos.Add(Med(3, "Tempra Forte"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 3, Hoy.AddMonths(9), 10));

        var resultado = await ConsultarAsync(medicamentos, lotes, termino);

        Assert.Equal(esperados, resultado.Count);
        Assert.DoesNotContain(resultado, r => r.IdMedicamento == 3);
    }

    [Fact]
    public async Task La_consulta_no_modifica_lotes_ni_medicamentos()
    {
        var medicamentos = new MedicamentoRepositoryFalso();
        medicamentos.Medicamentos.Add(Med(1, "Tempra"));
        var lotes = new LoteRepositoryFalso();
        lotes.Lotes.Add(LoteDe(1, 1, Hoy.AddDays(-3), 15));

        await ConsultarAsync(medicamentos, lotes);
        await ConsultarAsync(medicamentos, lotes);

        var lote = Assert.Single(lotes.Lotes);
        Assert.Equal(15, lote.Stock);
        Assert.Empty(medicamentos.Actualizados);
    }

    [Fact]
    public async Task Sin_sesion_activa_lanza_NoAutenticadoException()
    {
        var handler = CrearHandler(new MedicamentoRepositoryFalso(), new LoteRepositoryFalso(), idUsuario: null);

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            handler.Handle(new ObtenerMedicamentosAgotadosQuery(null), CancellationToken.None));
    }
}
