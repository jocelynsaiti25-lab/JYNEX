using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.EntradaLote.Commands;
using EtbotFarma.Application.EntradaLote.Handlers;
using EtbotFarma.Application.EntradaLote.Queries;
using EtbotFarma.Application.EntradaLote.Validators;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.EntradaLote;

public class RegistrarEntradaLoteTests
{
    [Fact]
    public async Task Con_datos_validos_crea_lote_registra_bitacora_y_actualiza_existencias_y_estado_agotado()
    {
        // Escenarios 10 y 11: medicamento estaba agotado (0 existencias) y pasa a disponible
        var repoMed = new FalsoMedicamentoRepository();
        var med = Medicamento.Reconstruir(1, "Paracetamol", "Paracetamol", "Tabletas", "500 mg", false, 10, true, DateTime.UtcNow);
        repoMed.Medicamentos.Add(med);

        var repoLotes = new FalsoLoteRepository();
        var repoMov = new FalsoMovimientoRepository();
        var uow = new FalsoUnitOfWork();
        var reloj = new FalsoDateTimeProvider(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        var usuario = new FalsoUsuarioActual(7);

        var handler = new RegistrarEntradaLoteHandler(repoMed, repoLotes, repoMov, usuario, uow, reloj);
        var command = new RegistrarEntradaLoteCommand(1, "LOT-2026-B", "2027-12-31", "25");

        var dto = await handler.Handle(command, CancellationToken.None);

        Assert.Single(repoLotes.LotesGuardados);
        Assert.Single(repoMov.MovimientosGuardados);
        Assert.Equal(Movimiento.TipoEntrada, repoMov.MovimientosGuardados[0].Tipo);
        Assert.Equal(7, repoMov.MovimientosGuardados[0].IdUsuario);
        Assert.Equal(25, dto.ExistenciasTotalesActualizadas);
        Assert.False(dto.EstaAgotado);
        Assert.False(dto.EnStockBajo);
        Assert.Equal("Verde", dto.SemaforoLote);
        Assert.True(uow.Confirmado);
    }

    [Fact]
    public async Task Con_medicamento_inactivo_lanza_excepcion_y_no_guarda_nada()
    {
        // Escenario 2
        var repoMed = new FalsoMedicamentoRepository();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Naproxeno", "Naproxeno", "Tabletas", "250 mg", false, 5, false, DateTime.UtcNow));

        var repoLotes = new FalsoLoteRepository();
        var repoMov = new FalsoMovimientoRepository();
        var uow = new FalsoUnitOfWork();
        var reloj = new FalsoDateTimeProvider(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));

        var handler = new RegistrarEntradaLoteHandler(repoMed, repoLotes, repoMov, new FalsoUsuarioActual(1), uow, reloj);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RegistrarEntradaLoteCommand(2, "LOT-99", "2027-08-01", "10"), CancellationToken.None));

        Assert.Empty(repoLotes.LotesGuardados);
        Assert.Empty(repoMov.MovimientosGuardados);
        Assert.False(uow.Confirmado);
    }

    [Fact]
    public async Task Con_numero_de_lote_duplicado_en_el_mismo_medicamento_lanza_excepcion()
    {
        // Escenarios 6 y 7
        var repoMed = new FalsoMedicamentoRepository();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Paracetamol", "Paracetamol", "Tabletas", "500 mg", false, 5, true, DateTime.UtcNow));
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Ibuprofeno", "Ibuprofeno", "Cápsulas", "400 mg", false, 5, true, DateTime.UtcNow));

        var repoLotes = new FalsoLoteRepository();
        repoLotes.LotesGuardados.Add(
            Lote.Reconstruir(10, 1, "LOT-2026-A", new DateOnly(2027, 5, 1), 20, new DateOnly(2026, 11, 1)));

        var repoMov = new FalsoMovimientoRepository();
        var uow = new FalsoUnitOfWork();
        var reloj = new FalsoDateTimeProvider(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));

        var handler = new RegistrarEntradaLoteHandler(repoMed, repoLotes, repoMov, new FalsoUsuarioActual(1), uow, reloj);

        // Debe fallar en medicamento 1
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RegistrarEntradaLoteCommand(1, "LOT-2026-A", "2027-09-01", "15"), CancellationToken.None));

        // Debe permitir el mismo código de lote en medicamento 2
        var ok = await handler.Handle(
            new RegistrarEntradaLoteCommand(2, "LOT-2026-A", "2027-09-01", "15"), CancellationToken.None);

        Assert.Equal("LOT-2026-A", ok.NumeroLote);
        Assert.Equal(2, ok.IdMedicamento);
    }

    [Fact]
    public void Validator_rechaza_campos_vacios_cantidad_cero_decimal_y_fecha_invalida()
    {
        // Escenarios 3, 4 y 5
        var validator = new RegistrarEntradaLoteValidator();

        var invalido = validator.Validate(new RegistrarEntradaLoteCommand(0, "", "2026-02-30", "0"));
        Assert.False(invalido.IsValid);
        Assert.True(invalido.Errors.Count >= 4);

        var decimalInvalido = validator.Validate(new RegistrarEntradaLoteCommand(1, "LOT-1", "2027-05-01", "2.5"));
        Assert.False(decimalInvalido.IsValid);
    }

    [Fact]
    public async Task Buscar_medicamentos_activos_excluye_inactivos_y_calcula_agotados()
    {
        // Escenarios 1, 2 y 11
        var repoMed = new FalsoMedicamentoRepository();
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(1, "Amoxicilina", "Amoxicilina", "Cápsulas", "500 mg", false, 10, true, DateTime.UtcNow));
        repoMed.Medicamentos.Add(
            Medicamento.Reconstruir(2, "Obsoleto", "Sustancia X", "Jarabe", "100 ml", false, 5, false, DateTime.UtcNow));

        var repoLotes = new FalsoLoteRepository();
        var reloj = new FalsoDateTimeProvider(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        var handler = new BuscarMedicamentosActivosHandler(repoMed, repoLotes, new FalsoUsuarioActual(1), reloj);

        var lista = await handler.Handle(new BuscarMedicamentosActivosQuery(""), CancellationToken.None);

        Assert.Single(lista);
        Assert.Equal("Amoxicilina", lista[0].NombreComercial);
        Assert.True(lista[0].EstaAgotado);
    }

    private sealed class FalsoMedicamentoRepository : IMedicamentoRepository
    {
        public List<Medicamento> Medicamentos { get; } = [];
        public Task<bool> ExisteDuplicadoAsync(string nc, string ns, string p, string c, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<int> AgregarAsync(Medicamento m, CancellationToken ct) => Task.FromResult(1);
        public Task<Medicamento?> ObtenerPorIdAsync(int idMedicamento, CancellationToken ct) =>
            Task.FromResult(Medicamentos.FirstOrDefault(m => m.Id == idMedicamento));
        public Task<IReadOnlyList<Medicamento>> BuscarActivosAsync(string? termino, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Medicamento>>(Medicamentos.Where(m => m.Activo).ToList());
    }

    private sealed class FalsoLoteRepository : ILoteRepository
    {
        public List<Lote> LotesGuardados { get; } = [];
        public Task<int> AgregarAsync(Lote lote, CancellationToken ct)
        {
            var id = LotesGuardados.Count + 1;
            lote.AsignarId(id);
            LotesGuardados.Add(lote);
            return Task.FromResult(id);
        }
        public Task<bool> ExisteNumeroLoteEnMedicamentoAsync(int idMedicamento, string numeroLote, CancellationToken ct) =>
            Task.FromResult(LotesGuardados.Any(l =>
                l.IdMedicamento == idMedicamento
                && l.NumeroLote.Equals(numeroLote.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyList<Lote>> ObtenerPorMedicamentoAsync(int idMedicamento, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Lote>>(LotesGuardados.Where(l => l.IdMedicamento == idMedicamento).ToList());
    }

    private sealed class FalsoMovimientoRepository : IMovimientoRepository
    {
        public List<Movimiento> MovimientosGuardados { get; } = [];
        public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken ct)
        {
            MovimientosGuardados.Add(movimiento);
            return Task.FromResult(1);
        }
    }

    private sealed class FalsoUnitOfWork : IUnitOfWork
    {
        public bool Confirmado { get; private set; }
        public Task GuardarCambiosAsync(CancellationToken ct)
        {
            Confirmado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FalsoUsuarioActual(int? idUsuario) : IUsuarioActual
    {
        public int? IdUsuario => idUsuario;
        public string NombreUsuario => "empleado.test";
        public string NombreCompleto => "Empleado de Prueba";
        public string Rol => "Empleado";
    }

    private sealed class FalsoDateTimeProvider(DateTimeOffset ahora) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => ahora;
    }
}