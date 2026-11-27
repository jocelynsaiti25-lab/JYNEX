using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Infrastructure.Services;

public sealed class GeneradorExcelBitacora(ConfiguracionReloj reloj) : IGeneradorExcelBitacora
{
    public const string TipoContenido = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const int EstiloFecha = 1;
    private const int EstiloHora = 2;
    private const int EstiloEncabezado = 3;

    private static readonly DateTime OrigenExcel = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);

    private static readonly string[] Encabezados =
    [
        "Fecha", "Hora", "Usuario", "Tipo de movimiento", "Medicamento", "Lote", "Cantidad", "Motivo"
    ];

    private const string TiposDeContenido = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
        <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
        <Default Extension="xml" ContentType="application/xml"/>
        <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
        <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
        <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private const string RelacionesRaiz = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string Libro = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
        <sheets><sheet name="Bitácora" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """;

    private const string RelacionesLibro = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
        <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
        <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private const string Estilos = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
        <numFmts count="2"><numFmt numFmtId="164" formatCode="dd/mm/yyyy"/><numFmt numFmtId="165" formatCode="hh:mm:ss"/></numFmts>
        <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
        <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
        <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
        <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
        <cellXfs count="4">
        <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
        <xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
        <xf numFmtId="165" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
        <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
        </cellXfs>
        <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    public byte[] Generar(IReadOnlyList<MovimientoBitacoraDto> movimientos)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(reloj.ZonaHoraria);

        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            Escribir(zip, "[Content_Types].xml", TiposDeContenido);
            Escribir(zip, "_rels/.rels", RelacionesRaiz);
            Escribir(zip, "xl/workbook.xml", Libro);
            Escribir(zip, "xl/_rels/workbook.xml.rels", RelacionesLibro);
            Escribir(zip, "xl/styles.xml", Estilos);
            Escribir(zip, "xl/worksheets/sheet1.xml", ConstruirHoja(movimientos, zona));
        }

        return memoria.ToArray();
    }

    private static void Escribir(ZipArchive zip, string ruta, string contenido)
    {
        var entrada = zip.CreateEntry(ruta, CompressionLevel.Optimal);
        using var escritor = new StreamWriter(entrada.Open(), new UTF8Encoding(false));
        escritor.Write(contenido);
    }

    private static string ConstruirHoja(IReadOnlyList<MovimientoBitacoraDto> movimientos, TimeZoneInfo zona)
    {
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        xml.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        xml.Append("<cols>");
        xml.Append("<col min=\"1\" max=\"2\" width=\"12\" customWidth=\"1\"/>");
        xml.Append("<col min=\"3\" max=\"3\" width=\"28\" customWidth=\"1\"/>");
        xml.Append("<col min=\"4\" max=\"4\" width=\"20\" customWidth=\"1\"/>");
        xml.Append("<col min=\"5\" max=\"6\" width=\"36\" customWidth=\"1\"/>");
        xml.Append("<col min=\"7\" max=\"7\" width=\"10\" customWidth=\"1\"/>");
        xml.Append("<col min=\"8\" max=\"8\" width=\"40\" customWidth=\"1\"/>");
        xml.Append("</cols>");
        xml.Append("<sheetData>");

        xml.Append("<row r=\"1\">");
        for (var columna = 0; columna < Encabezados.Length; columna++)
        {
            AgregarTexto(xml, Referencia(columna, 1), Encabezados[columna], EstiloEncabezado);
        }
        xml.Append("</row>");

        for (var indice = 0; indice < movimientos.Count; indice++)
        {
            var movimiento = movimientos[indice];
            var fila = indice + 2;
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(movimiento.FechaUtc, DateTimeKind.Utc), zona);

            xml.Append("<row r=\"").Append(fila.ToString(CultureInfo.InvariantCulture)).Append("\">");
            AgregarNumero(xml, Referencia(0, fila), (local.Date - OrigenExcel).TotalDays, EstiloFecha);
            AgregarNumero(xml, Referencia(1, fila), local.TimeOfDay.TotalDays, EstiloHora);
            AgregarTexto(xml, Referencia(2, fila), movimiento.Usuario);
            AgregarTexto(xml, Referencia(3, fila), movimiento.Tipo);
            AgregarTexto(xml, Referencia(4, fila), NombreCompleto(movimiento));
            AgregarTexto(xml, Referencia(5, fila), Lotes(movimiento));
            AgregarNumero(xml, Referencia(6, fila), movimiento.CantidadTotal);
            AgregarTexto(xml, Referencia(7, fila), movimiento.Motivo ?? string.Empty);
            xml.Append("</row>");
        }

        xml.Append("</sheetData></worksheet>");
        return xml.ToString();
    }

    private static string NombreCompleto(MovimientoBitacoraDto movimiento) =>
        $"{movimiento.Medicamento} {movimiento.Presentacion} {movimiento.Concentracion}";

    private static string Lotes(MovimientoBitacoraDto movimiento) =>
        string.Join("; ", movimiento.Lotes.Select(l => $"{l.NumeroLote} ({l.Cantidad.ToString(CultureInfo.InvariantCulture)})"));

    private static string Referencia(int columna, int fila) =>
        $"{(char)('A' + columna)}{fila.ToString(CultureInfo.InvariantCulture)}";

    private static void AgregarNumero(StringBuilder xml, string referencia, double valor, int estilo = 0)
    {
        xml.Append("<c r=\"").Append(referencia).Append('"');
        if (estilo != 0)
        {
            xml.Append(" s=\"").Append(estilo.ToString(CultureInfo.InvariantCulture)).Append('"');
        }

        xml.Append("><v>").Append(valor.ToString("0.##########", CultureInfo.InvariantCulture)).Append("</v></c>");
    }

    private static void AgregarTexto(StringBuilder xml, string referencia, string texto, int estilo = 0)
    {
        xml.Append("<c r=\"").Append(referencia).Append("\" t=\"inlineStr\"");
        if (estilo != 0)
        {
            xml.Append(" s=\"").Append(estilo.ToString(CultureInfo.InvariantCulture)).Append('"');
        }

        xml.Append("><is><t xml:space=\"preserve\">").Append(Escapar(texto)).Append("</t></is></c>");
    }

    private static string Escapar(string texto)
    {
        var limpio = new StringBuilder(texto.Length);
        foreach (var caracter in texto)
        {
            if (!XmlConvert.IsXmlChar(caracter) && !char.IsSurrogate(caracter))
            {
                continue;
            }

            limpio.Append(caracter switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => caracter.ToString()
            });
        }

        return limpio.ToString();
    }
}
