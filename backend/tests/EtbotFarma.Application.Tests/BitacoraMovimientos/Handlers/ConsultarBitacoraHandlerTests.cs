using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.BitacoraMovimientos.Handlers;
using EtbotFarma.Application.BitacoraMovimientos.Queries;
using EtbotFarma.Application.Common.Configuracion;
using EtbotFarma.Application.Tests.Falsos;

namespace EtbotFarma.Application.Tests.BitacoraMovimientos.Handlers;

public class ConsultarBitacoraHandlerTests
{
    private readonly MovimientoRepositoryFalso _movimientos = new();

    private ConsultarBitacoraHandler Handler(string zona = "UTC") =>
        new(_movimientos, new ConfiguracionReloj { ZonaHoraria = zona });

    private static ConsultarBitacoraQuery Consulta(
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? idUsuario = null,
        int? idMedicamento = null,
        string? tipo = null,
        int pagina = 1,
        int tamanoPagina = 20) =>
        new(desde, hasta, idUsuario, idMedicamento, tipo, pagina, tamanoPagina);

    private void Agregar(int id, DateTime fechaUtc, string tipo = "ENTRADA", int idUsuario = 1, int idMedicamento = 10)
        => _movimientos.Bitacora.Add(new MovimientoBitacoraDto(
            id, fechaUtc, idUsuario, $"Usuario {idUsuario}", tipo, idMedicamento, $"Medicamento {idMedicamento}",
            "Tabletas", "500 mg", 5, tipo == "BAJA" ? "Caducado" : null, []));

    private static DateTime Utc(int anio, int mes, int dia, int hora = 0, int minuto = 0) =>
        new(anio, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Sin_filtros_devuelve_del_mas_reciente_al_mas_antiguo()
    {
        Agregar(1, Utc(2026, 11, 1, 8));
        Agregar(2, Utc(2026, 11, 3, 8));
        Agregar(3, Utc(2026, 11, 2, 8));

        var pagina = await Handler().Handle(Consulta(), CancellationToken.None);

        Assert.Equal(new[] { 2, 3, 1 }, pagina.Movimientos.Select(m => m.IdMovimiento));
        Assert.Equal(3, pagina.Total);
    }

    [Fact]
    public async Task Con_la_misma_fecha_desempata_por_id_descendente()
    {
        Agregar(1, Utc(2026, 11, 1, 8));
        Agregar(2, Utc(2026, 11, 1, 8));

        var pagina = await Handler().Handle(Consulta(), CancellationToken.None);

        Assert.Equal(new[] { 2, 1 }, pagina.Movimientos.Select(m => m.IdMovimiento));
    }

    [Fact]
    public async Task Rango_de_fechas_incluye_el_dia_inicial_y_el_final()
    {
        Agregar(1, Utc(2026, 11, 9, 23, 59));
        Agregar(2, Utc(2026, 11, 10, 0, 0));
        Agregar(3, Utc(2026, 11, 12, 23, 59));
        Agregar(4, Utc(2026, 11, 13, 0, 0));

        var pagina = await Handler().Handle(
            Consulta(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 12)), CancellationToken.None);

        Assert.Equal(new[] { 3, 2 }, pagina.Movimientos.Select(m => m.IdMovimiento));
    }

    [Fact]
    public async Task Las_fechas_se_convierten_desde_la_zona_horaria_de_la_farmacia()
    {
        Agregar(1, Utc(2026, 11, 10, 5, 59));
        Agregar(2, Utc(2026, 11, 10, 6, 0));
        Agregar(3, Utc(2026, 11, 11, 5, 59));
        Agregar(4, Utc(2026, 11, 11, 6, 0));

        var pagina = await Handler("America/Mexico_City").Handle(
            Consulta(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 10)), CancellationToken.None);

        Assert.Equal(new[] { 3, 2 }, pagina.Movimientos.Select(m => m.IdMovimiento));
    }

    [Fact]
    public async Task Filtros_combinados_exigen_cumplir_todos_a_la_vez()
    {
        Agregar(1, Utc(2026, 11, 5), "BAJA", idUsuario: 2, idMedicamento: 7);
        Agregar(2, Utc(2026, 11, 5), "BAJA", idUsuario: 3, idMedicamento: 7);
        Agregar(3, Utc(2026, 11, 5), "SALIDA", idUsuario: 2, idMedicamento: 7);
        Agregar(4, Utc(2026, 11, 5), "BAJA", idUsuario: 2, idMedicamento: 8);
        Agregar(5, Utc(2026, 10, 5), "BAJA", idUsuario: 2, idMedicamento: 7);

        var pagina = await Handler().Handle(
            Consulta(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), 2, 7, "BAJA"), CancellationToken.None);

        Assert.Equal(new[] { 1 }, pagina.Movimientos.Select(m => m.IdMovimiento));
    }

    [Fact]
    public async Task El_tipo_se_normaliza_a_mayusculas_sin_espacios()
    {
        Agregar(1, Utc(2026, 11, 5), "SALIDA");
        Agregar(2, Utc(2026, 11, 5), "ENTRADA");

        var pagina = await Handler().Handle(Consulta(tipo: " salida "), CancellationToken.None);

        Assert.Equal("SALIDA", _movimientos.UltimoFiltro!.Tipo);
        Assert.Equal(new[] { 1 }, pagina.Movimientos.Select(m => m.IdMovimiento));
    }

    [Fact]
    public async Task Sin_coincidencias_devuelve_lista_vacia_sin_error()
    {
        Agregar(1, Utc(2026, 11, 5), "ENTRADA");

        var pagina = await Handler().Handle(Consulta(tipo: "BAJA"), CancellationToken.None);

        Assert.Empty(pagina.Movimientos);
        Assert.Equal(0, pagina.Total);
        Assert.Equal(0, pagina.TotalPaginas);
    }

    [Fact]
    public async Task La_paginacion_conserva_el_orden_y_calcula_el_total_de_paginas()
    {
        for (var id = 1; id <= 5; id++)
        {
            Agregar(id, Utc(2026, 11, id));
        }

        var segunda = await Handler().Handle(Consulta(pagina: 2, tamanoPagina: 2), CancellationToken.None);

        Assert.Equal(new[] { 3, 2 }, segunda.Movimientos.Select(m => m.IdMovimiento));
        Assert.Equal(5, segunda.Total);
        Assert.Equal(3, segunda.TotalPaginas);
        Assert.Equal(2, segunda.Pagina);
    }

    [Fact]
    public async Task Medicamentos_del_filtro_se_obtienen_del_repositorio()
    {
        _movimientos.MedicamentosDeBitacora.Add(new MedicamentoBitacoraDto(1, "Paracetamol", "Paracetamol", "Tabletas", "500 mg"));

        var lista = await new ConsultarMedicamentosBitacoraHandler(_movimientos)
            .Handle(new ConsultarMedicamentosBitacoraQuery(), CancellationToken.None);

        Assert.Single(lista);
        Assert.Equal("Paracetamol", lista[0].NombreComercial);
    }
}
