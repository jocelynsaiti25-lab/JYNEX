using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Handlers;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;

namespace EtbotFarma.Application.Tests.BitacoraMovimientos.Handlers;

public class ExportarBitacoraHandlerTests
{
    private readonly MovimientoRepositoryFalso _movimientos = new();
    private readonly GeneradorExcelBitacoraFalso _generador = new();

    private ExportarBitacoraHandler Handler(string zona = "UTC") =>
        new(_movimientos, _generador, new ConfiguracionReloj { ZonaHoraria = zona },
            new RelojFalso(new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero)));

    private static ExportarBitacoraQuery Consulta(
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? idUsuario = null,
        int? idMedicamento = null,
        string? tipo = null) =>
        new(desde, hasta, idUsuario, idMedicamento, tipo);

    private void Agregar(int id, DateTime fechaUtc, string tipo = "ENTRADA", int idUsuario = 1, int idMedicamento = 10)
        => _movimientos.Bitacora.Add(new MovimientoBitacoraDto(
            id, fechaUtc, idUsuario, $"Usuario {idUsuario}", tipo, idMedicamento, $"Medicamento {idMedicamento}",
            "Tabletas", "500 mg", 5, tipo == "BAJA" ? "Caducado" : null, []));

    private static DateTime Utc(int anio, int mes, int dia, int hora = 0) =>
        new(anio, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Exporta_todos_los_movimientos_aunque_superen_un_lote_de_lectura()
    {
        var cantidad = ExportarBitacoraHandler.TamanoLote * 2 + 37;
        for (var id = 1; id <= cantidad; id++)
        {
            Agregar(id, Utc(2026, 11, 1).AddMinutes(id));
        }

        var archivo = await Handler().Handle(Consulta(), CancellationToken.None);

        Assert.Equal(cantidad, archivo.TotalMovimientos);
        Assert.Equal(cantidad, _generador.Recibidos.Count);
        Assert.Equal(cantidad, _generador.Recibidos.Select(m => m.IdMovimiento).Distinct().Count());
        Assert.Equal(1, _generador.Llamadas);
    }

    [Fact]
    public async Task Respeta_los_filtros_y_no_exporta_toda_la_bitacora()
    {
        Agregar(1, Utc(2026, 11, 5), "BAJA", idUsuario: 2, idMedicamento: 7);
        Agregar(2, Utc(2026, 11, 5), "BAJA", idUsuario: 3, idMedicamento: 7);
        Agregar(3, Utc(2026, 11, 5), "SALIDA", idUsuario: 2, idMedicamento: 7);
        Agregar(4, Utc(2026, 10, 5), "BAJA", idUsuario: 2, idMedicamento: 7);

        var archivo = await Handler().Handle(
            Consulta(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), 2, 7, " baja "), CancellationToken.None);

        Assert.Equal(1, archivo.TotalMovimientos);
        Assert.Equal(new[] { 1 }, _generador.Recibidos.Select(m => m.IdMovimiento));
    }

    [Fact]
    public async Task Las_fechas_se_convierten_con_la_zona_horaria_de_la_farmacia()
    {
        Agregar(1, Utc(2026, 11, 10, 5));
        Agregar(2, Utc(2026, 11, 10, 6));
        Agregar(3, Utc(2026, 11, 11, 5));
        Agregar(4, Utc(2026, 11, 11, 6));

        var archivo = await Handler("America/Mexico_City").Handle(
            Consulta(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 10)), CancellationToken.None);

        Assert.Equal(2, archivo.TotalMovimientos);
        Assert.Equal(new[] { 3, 2 }, _generador.Recibidos.Select(m => m.IdMovimiento));
    }

    [Fact]
    public async Task El_nombre_del_archivo_es_descriptivo_y_lleva_la_fecha_de_generacion()
    {
        Agregar(1, Utc(2026, 11, 5));

        var archivo = await Handler().Handle(Consulta(), CancellationToken.None);

        Assert.Equal("bitacora-movimientos-2026-11-27.xlsx", archivo.NombreArchivo);
        Assert.NotEmpty(archivo.Contenido);
    }

    [Fact]
    public async Task Sin_movimientos_se_informa_y_no_se_genera_archivo()
    {
        Agregar(1, Utc(2026, 11, 5), "ENTRADA");

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            Handler().Handle(Consulta(tipo: "BAJA"), CancellationToken.None));

        Assert.Equal(ExportarBitacoraHandler.MensajeSinMovimientos, error.Message);
        Assert.Equal(0, _generador.Llamadas);
    }
}
