using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.EdicionMedicamentos.Handlers;
using EtbotFarma.Application.EdicionMedicamentos.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.EdicionMedicamentos.Handlers;

/// <summary>Pruebas de HU-09: datos del medicamento para precargar el formulario de edición.</summary>
public class ObtenerMedicamentoParaEditarHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);

    private readonly MedicamentoRepositoryFalso _medicamentos = new();
    private readonly LoteRepositoryFalso _lotes = new();
    private readonly ObtenerMedicamentoParaEditarHandler _handler;

    public ObtenerMedicamentoParaEditarHandlerTests()
    {
        _medicamentos.Medicamentos.Add(Medicamento.Reconstruir(
            1, "Tempra", "Paracetamol", "Tabletas", "500 mg", esControlado: false, stockMinimo: 5, activo: true,
            Ahora.UtcDateTime));

        _lotes.Lotes.Add(Lote.Reconstruir(1, 1, "LOTE-01", new DateOnly(2028, 1, 1), 20, new DateOnly(2026, 5, 5)));

        _handler = new ObtenerMedicamentoParaEditarHandler(_medicamentos, _lotes, new RelojFalso(Ahora));
    }

    /// <summary>Escenario 2: trae los campos descriptivos actuales para precargar el formulario.</summary>
    [Fact]
    public async Task Devuelve_los_datos_descriptivos_actuales()
    {
        var resultado = await _handler.Handle(new ObtenerMedicamentoParaEditarQuery(1), CancellationToken.None);

        Assert.Equal("Tempra", resultado.NombreComercial);
        Assert.Equal("Paracetamol", resultado.NombreSustancia);
        Assert.Equal("Tabletas", resultado.Presentacion);
        Assert.Equal("500 mg", resultado.Concentracion);
        Assert.False(resultado.EsControlado);
    }

    /// <summary>Escenario 10: las existencias llegan solo como referencia (no se editan aquí).</summary>
    [Fact]
    public async Task Incluye_las_existencias_solo_como_referencia()
    {
        var resultado = await _handler.Handle(new ObtenerMedicamentoParaEditarQuery(1), CancellationToken.None);

        Assert.Equal(20, resultado.ExistenciasTotales);
        Assert.Equal("Verde", resultado.SemaforoCaducidad);
    }

    /// <summary>Un id que no existe (dirección escrita a mano) → 404 "El medicamento no existe".</summary>
    [Fact]
    public async Task Con_un_medicamento_que_no_existe_lanza_NoEncontradoException()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => _handler.Handle(new ObtenerMedicamentoParaEditarQuery(999), CancellationToken.None));

        Assert.Equal("El medicamento no existe", ex.Message);
    }
}
