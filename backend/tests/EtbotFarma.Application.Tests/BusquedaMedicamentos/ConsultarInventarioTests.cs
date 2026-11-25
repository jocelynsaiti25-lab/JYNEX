using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.BusquedaMedicamentos.Handlers;
using EtbotFarma.Application.BusquedaMedicamentos.Queries;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.BusquedaMedicamentos;

public class ConsultarInventarioTests
{
    [Fact]
    public async Task Busca_por_nombre_sustancia_o_lote_sin_distinguir_mayusculas()
    {
        // HU-08 escenario 2
        var handler = CrearHandlerConDatosDePrueba();

        var porNombre = await handler.Handle(new ConsultarInventarioQuery("paracet", null), CancellationToken.None);
        var porSustancia = await handler.Handle(new ConsultarInventarioQuery("IBUPROFENO", null), CancellationToken.None);
        var porLote = await handler.Handle(new ConsultarInventarioQuery("lot-2026-b", null), CancellationToken.None);

        Assert.Equal(2, porNombre.Count);
        Assert.Single(porSustancia);
        Assert.Single(porLote);
        Assert.Equal("LOT-2026-B", porLote[0].NumeroLote);
    }

    [Fact]
    public async Task Ordena_multiples_lotes_por_fecha_de_caducidad_ascendente()
    {
        // HU-08 escenario 4
        var handler = CrearHandlerConDatosDePrueba();

        var resultados = await handler.Handle(new ConsultarInventarioQuery("Paracetamol", null), CancellationToken.None);

        Assert.Equal(2, resultados.Count);
        Assert.Equal("LOT-PROXIMO", resultados[0].NumeroLote);
        Assert.Equal("LOT-LEJANO", resultados[1].NumeroLote);
        Assert.True(resultados[0].FechaCaducidad < resultados[1].FechaCaducidad);
    }

    [Fact]
    public async Task Combina_busqueda_textual_con_filtro_de_semaforo_de_caducidad()
    {
        // HU-08 escenario 5
        var handler = CrearHandlerConDatosDePrueba();

        var combinados = await handler.Handle(new ConsultarInventarioQuery("Paracetamol", "Rojo"), CancellationToken.None);

        Assert.Single(combinados);
        Assert.Equal("LOT-PROXIMO", combinados[0].NumeroLote);
        Assert.Equal("Rojo", combinados[0].SemaforoCaducidad);
    }

    [Fact]
    public async Task Sin_coincidencias_devuelve_lista_vacia_sin_error()
    {
        // HU-08 escenario 6
        var handler = CrearHandlerConDatosDePrueba();

        var vacio = await handler.Handle(new ConsultarInventarioQuery("MedicamentoInexistenteXYZ", "Verde"), CancellationToken.None);

        Assert.Empty(vacio);
    }

    [Fact]
    public async Task Sin_sesion_activa_lanza_NoAutenticadoException()
    {
        // HU-08 escenario 8
        var handler = CrearHandlerConDatosDePrueba(idUsuario: null);

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            handler.Handle(new ConsultarInventarioQuery(null, null), CancellationToken.None));
    }

    private static ConsultarInventarioHandler CrearHandlerConDatosDePrueba(int? idUsuario = 1)
    {
        var hoy = new DateOnly(2026, 11, 25);
        var repoMed = new MedicamentoRepositoryFalso();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Paracetamol Genérico", "Paracetamol", "Tabletas", "500 mg", false, 10, true, DateTime.UtcNow));
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Advil", "Ibuprofeno", "Cápsulas", "400 mg", false, 5, true, DateTime.UtcNow));

        var repoLotes = new LoteRepositoryFalso();
        repoLotes.Lotes.Add(Lote.Reconstruir(1, 1, "LOT-LEJANO", hoy.AddMonths(15), 40, hoy));
        repoLotes.Lotes.Add(Lote.Reconstruir(2, 1, "LOT-PROXIMO", hoy.AddMonths(2), 15, hoy));
        repoLotes.Lotes.Add(Lote.Reconstruir(3, 2, "LOT-2026-B", hoy.AddMonths(6), 30, hoy));

        var reloj = new FalsoDateTimeProvider(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        return new ConsultarInventarioHandler(repoMed, repoLotes, new FalsoUsuarioActual(idUsuario), reloj);
    }

    private sealed class FalsoUsuarioActual(int? idUsuario) : IUsuarioActual
    {
        public int? IdUsuario => idUsuario;
        public string NombreUsuario => "usuario.test";
        public string NombreCompleto => "Usuario Prueba";
        public string Rol => "Empleado";
    }

    private sealed class FalsoDateTimeProvider(DateTimeOffset ahora) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => ahora;
    }
}