using System.IO.Compression;
using System.Xml.Linq;
using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Infrastructure.Services;

namespace EtbotFarma.Api.IntegrationTests;

public class GeneradorExcelBitacoraTests
{
    private static readonly XNamespace Hoja = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static MovimientoBitacoraDto Movimiento(
        int id,
        DateTime fechaUtc,
        string usuario = "María Núñez",
        string tipo = "SALIDA",
        string medicamento = "Paracetamol",
        int cantidad = 15,
        string? motivo = null,
        params DetalleBitacoraDto[] lotes) =>
        new(id, fechaUtc, 1, usuario, tipo, 10, medicamento, "Tabletas", "500 mg", cantidad, motivo, lotes);

    private static byte[] Generar(string zona, params MovimientoBitacoraDto[] movimientos) =>
        new GeneradorExcelBitacora(new ConfiguracionReloj { ZonaHoraria = zona }).Generar(movimientos);

    private static XDocument LeerParte(byte[] archivo, string ruta)
    {
        using var zip = new ZipArchive(new MemoryStream(archivo), ZipArchiveMode.Read);
        var entrada = zip.GetEntry(ruta) ?? throw new InvalidOperationException($"Falta {ruta} en el archivo.");
        using var flujo = entrada.Open();
        return XDocument.Load(flujo);
    }

    private static List<XElement> Celdas(byte[] archivo, int fila) =>
        LeerParte(archivo, "xl/worksheets/sheet1.xml")
            .Descendants(Hoja + "row")
            .Single(r => (int)r.Attribute("r")! == fila)
            .Elements(Hoja + "c")
            .ToList();

    private static string Texto(XElement celda) => celda.Descendants(Hoja + "t").Single().Value;

    private static double Numero(XElement celda) =>
        double.Parse(celda.Element(Hoja + "v")!.Value, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void El_archivo_es_un_libro_valido_con_todas_sus_partes_en_xml_bien_formado()
    {
        var archivo = Generar("UTC", Movimiento(1, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc)));

        using var zip = new ZipArchive(new MemoryStream(archivo), ZipArchiveMode.Read);
        var partes = new[]
        {
            "[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/_rels/workbook.xml.rels",
            "xl/styles.xml", "xl/worksheets/sheet1.xml"
        };

        foreach (var parte in partes)
        {
            Assert.NotNull(zip.GetEntry(parte));
            Assert.NotNull(LeerParte(archivo, parte).Root);
        }
    }

    [Fact]
    public void La_primera_fila_trae_los_encabezados_y_los_datos_van_debajo()
    {
        var archivo = Generar("UTC", Movimiento(1, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc)));

        var encabezados = Celdas(archivo, 1).Select(Texto).ToArray();

        Assert.Equal(
            new[] { "Fecha", "Hora", "Usuario", "Tipo de movimiento", "Medicamento", "Lote", "Cantidad", "Motivo" },
            encabezados);
        Assert.Equal(8, Celdas(archivo, 2).Count);
    }

    [Fact]
    public void Fecha_hora_y_cantidad_son_numeros_con_formato_para_ordenar_y_sumar()
    {
        var archivo = Generar("UTC", Movimiento(1, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc), cantidad: 15));

        var celdas = Celdas(archivo, 2);

        Assert.Null(celdas[0].Attribute("t"));
        Assert.Equal(46353, Numero(celdas[0]));
        Assert.Equal("1", celdas[0].Attribute("s")!.Value);
        Assert.Null(celdas[1].Attribute("t"));
        Assert.Equal(0.5, Numero(celdas[1]), 6);
        Assert.Equal("2", celdas[1].Attribute("s")!.Value);
        Assert.Null(celdas[6].Attribute("t"));
        Assert.Equal(15, Numero(celdas[6]));
    }

    [Fact]
    public void Los_formatos_de_fecha_y_hora_estan_definidos_en_los_estilos()
    {
        var archivo = Generar("UTC", Movimiento(1, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc)));

        var estilos = LeerParte(archivo, "xl/styles.xml");
        var formatos = estilos.Descendants(Hoja + "numFmt").Select(n => n.Attribute("formatCode")!.Value).ToArray();

        Assert.Contains("dd/mm/yyyy", formatos);
        Assert.Contains("hh:mm:ss", formatos);
    }

    [Fact]
    public void La_fecha_y_la_hora_usan_la_zona_horaria_de_la_farmacia()
    {
        var archivo = Generar("America/Mexico_City", Movimiento(1, new DateTime(2026, 11, 27, 3, 0, 0, DateTimeKind.Utc)));

        var celdas = Celdas(archivo, 2);

        Assert.Equal(46352, Numero(celdas[0]));
        Assert.Equal(21.0 / 24.0, Numero(celdas[1]), 6);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://malicioso\",\"clic\")")]
    [InlineData("+SUMA(1;1)")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public void Un_texto_que_parece_formula_se_guarda_como_texto_y_nunca_como_formula(string motivo)
    {
        var archivo = Generar("UTC",
            Movimiento(1, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc), usuario: motivo, tipo: "BAJA", motivo: motivo));

        var hoja = LeerParte(archivo, "xl/worksheets/sheet1.xml");
        var celdas = Celdas(archivo, 2);

        Assert.Empty(hoja.Descendants(Hoja + "f"));
        Assert.Equal("inlineStr", celdas[7].Attribute("t")!.Value);
        Assert.Equal(motivo, Texto(celdas[7]));
        Assert.Equal("inlineStr", celdas[2].Attribute("t")!.Value);
        Assert.Equal(motivo, Texto(celdas[2]));
    }

    [Fact]
    public void Acentos_enes_simbolos_y_caracteres_xml_se_conservan_sin_danar_el_archivo()
    {
        const string motivo = "Ñandú & café <b> \"3\" 'x' ° € 5% — ¿qué?";
        var archivo = Generar("UTC",
            Movimiento(1, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc), usuario: "José Peña", tipo: "BAJA", motivo: motivo));

        var celdas = Celdas(archivo, 2);

        Assert.Equal("José Peña", Texto(celdas[2]));
        Assert.Equal(motivo, Texto(celdas[7]));
    }

    [Fact]
    public void Los_caracteres_de_control_que_romperian_el_xml_se_eliminan()
    {
        var archivo = Generar("UTC",
            Movimiento(1, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc), tipo: "BAJA", motivo: "Dañado\u0001\u0008 en transporte"));

        Assert.Equal("Dañado en transporte", Texto(Celdas(archivo, 2)[7]));
    }

    [Fact]
    public void Una_salida_con_varios_lotes_es_una_sola_fila_con_el_detalle_de_cada_lote()
    {
        var archivo = Generar("UTC",
            Movimiento(
                1,
                new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc),
                cantidad: 15,
                motivo: null,
                lotes: [new DetalleBitacoraDto(1, "L-A", 10), new DetalleBitacoraDto(2, "L-B", 5)]));

        var hoja = LeerParte(archivo, "xl/worksheets/sheet1.xml");
        var celdas = Celdas(archivo, 2);

        Assert.Equal(2, hoja.Descendants(Hoja + "row").Count());
        Assert.Equal("L-A (10); L-B (5)", Texto(celdas[5]));
        Assert.Equal(15, Numero(celdas[6]));
        Assert.Equal(string.Empty, Texto(celdas[7]));
    }

    [Fact]
    public void Cada_movimiento_es_una_fila_en_el_orden_recibido()
    {
        var movimientos = Enumerable.Range(1, 600)
            .Select(i => Movimiento(i, new DateTime(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc), cantidad: i))
            .ToArray();

        var archivo = Generar("UTC", movimientos);

        var filas = LeerParte(archivo, "xl/worksheets/sheet1.xml").Descendants(Hoja + "row").ToList();
        Assert.Equal(601, filas.Count);
        Assert.Equal(1, Numero(Celdas(archivo, 2)[6]));
        Assert.Equal(600, Numero(Celdas(archivo, 601)[6]));
    }
}
