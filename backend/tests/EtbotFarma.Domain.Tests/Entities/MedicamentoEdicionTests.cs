using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Domain.Tests.Entities;

/// <summary>Pruebas de HU-09: edición de datos descriptivos del medicamento.</summary>
public sealed class MedicamentoEdicionTests
{
    private static Medicamento CrearMedicamento() => Medicamento.Reconstruir(
        1, "Tempra", "Paracetamol", "Tabletas", "500 mg", esControlado: false, stockMinimo: 5, activo: true,
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    /// <summary>Escenario 1: actualiza los campos descriptivos y conserva Id, stock mínimo y activo.</summary>
    [Fact]
    public void Editar_actualiza_los_datos_descriptivos_y_conserva_el_resto()
    {
        var medicamento = CrearMedicamento();

        medicamento.Editar("Tempra Forte", "Paracetamol", "Jarabe", "250 mg/5 ml", esControlado: true);

        Assert.Equal(1, medicamento.Id);
        Assert.Equal("Tempra Forte", medicamento.NombreComercial);
        Assert.Equal("Jarabe", medicamento.Presentacion);
        Assert.Equal("250 mg/5 ml", medicamento.Concentracion);
        Assert.True(medicamento.EsControlado);
        Assert.Equal(5, medicamento.StockMinimo);   // nunca se toca aquí
        Assert.True(medicamento.Activo);
    }

    /// <summary>Escenario 7: cada campo descriptivo obligatorio vacío lanza DomainException.</summary>
    [Theory]
    [InlineData("", "Paracetamol", "Tabletas", "500 mg")]
    [InlineData("Tempra", "   ", "Tabletas", "500 mg")]
    [InlineData("Tempra", "Paracetamol", "", "500 mg")]
    [InlineData("Tempra", "Paracetamol", "Tabletas", "")]
    public void Editar_con_un_campo_obligatorio_vacio_lanza_excepcion(
        string nombreComercial, string nombreSustancia, string presentacion, string concentracion)
    {
        var medicamento = CrearMedicamento();

        Assert.Throws<DomainException>(() =>
            medicamento.Editar(nombreComercial, nombreSustancia, presentacion, concentracion, esControlado: false));

        // Escenario 7: los datos anteriores permanecen intactos
        Assert.Equal("Tempra", medicamento.NombreComercial);
    }

    /// <summary>Escenario 12: sin ningún cambio real no hay nada que detectar.</summary>
    [Fact]
    public void TieneCambiosDescriptivos_con_los_mismos_valores_es_falso()
    {
        var medicamento = CrearMedicamento();

        Assert.False(medicamento.TieneCambiosDescriptivos("Tempra", "Paracetamol", "Tabletas", "500 mg", esControlado: false));
    }

    /// <summary>Escenario 9: cambiar solo "Medicamento controlado" ya cuenta como un cambio real.</summary>
    [Fact]
    public void TieneCambiosDescriptivos_detecta_cambio_solo_en_es_controlado()
    {
        var medicamento = CrearMedicamento();

        Assert.True(medicamento.TieneCambiosDescriptivos("Tempra", "Paracetamol", "Tabletas", "500 mg", esControlado: true));
    }

    /// <summary>Escenario 1: la auditoría nunca incluye stock, lotes ni fechas.</summary>
    [Fact]
    public void DatosParaAuditoria_no_incluye_stock_ni_fechas()
    {
        var medicamento = CrearMedicamento();

        var json = medicamento.DatosParaAuditoria();

        Assert.Contains("Tempra", json);
        Assert.DoesNotContain("stockMinimo", json);
        Assert.DoesNotContain("fechaRegistro", json);
    }
}
