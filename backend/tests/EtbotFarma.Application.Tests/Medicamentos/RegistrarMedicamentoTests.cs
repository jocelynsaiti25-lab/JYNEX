using EtbotFarma.Application.Commands.Medicamentos;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Handlers.Medicamentos;
using EtbotFarma.Application.Validators.Medicamentos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.Medicamentos;

public sealed class RegistrarMedicamentoTests
{
    [Fact]
    public async Task Con_unidades_iniciales_guarda_medicamento_lote_y_entrada_en_bitacora()
    {
        var medRepo = new MedicamentoRepositoryFalso();
        var loteRepo = new LoteRepositoryFalso();
        var movRepo = new MovimientoRepositoryFalso();
        var uow = new UnitOfWorkFalso();
        var handler = new RegistrarMedicamentoHandler(
            medRepo, loteRepo, movRepo, new UsuarioActualFalso(7), uow, new RelojFalso());

        var command = new RegistrarMedicamentoCommand(
            "Tempra", "Paracetamol", "Tabletas", "500 mg", false, "LOTE-2027", "2027-10-15", "50");

        var resultado = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Medicamento registrado", resultado.Mensaje);
        Assert.Equal(50, resultado.Stock);
        Assert.True(resultado.SeRegistroEntrada);
        Assert.Single(medRepo.Guardados);
        Assert.Single(loteRepo.Guardados);
        Assert.Single(movRepo.Guardados);
        Assert.Equal(50, movRepo.Guardados[0].CantidadTotal);
        Assert.True(uow.Confirmado);
    }

    [Fact]
    public async Task Con_cero_unidades_guarda_medicamento_y_lote_sin_generar_entrada_en_bitacora()
    {
        var medRepo = new MedicamentoRepositoryFalso();
        var loteRepo = new LoteRepositoryFalso();
        var movRepo = new MovimientoRepositoryFalso();
        var uow = new UnitOfWorkFalso();
        var handler = new RegistrarMedicamentoHandler(
            medRepo, loteRepo, movRepo, new UsuarioActualFalso(7), uow, new RelojFalso());

        var command = new RegistrarMedicamentoCommand(
            "Tempra", "Paracetamol", "Tabletas", "500 mg", true, "LOTE-00", "2027-10-15", "0");

        var resultado = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, resultado.Stock);
        Assert.False(resultado.SeRegistroEntrada);
        Assert.Single(medRepo.Guardados);
        Assert.Single(loteRepo.Guardados);
        Assert.Empty(movRepo.Guardados);
        Assert.True(uow.Confirmado);
    }

    [Fact]
    public async Task Con_medicamento_duplicado_lanza_excepcion_y_no_guarda_nada()
    {
        var medRepo = new MedicamentoRepositoryFalso { SimularDuplicado = true };
        var loteRepo = new LoteRepositoryFalso();
        var movRepo = new MovimientoRepositoryFalso();
        var uow = new UnitOfWorkFalso();
        var handler = new RegistrarMedicamentoHandler(
            medRepo, loteRepo, movRepo, new UsuarioActualFalso(7), uow, new RelojFalso());

        var command = new RegistrarMedicamentoCommand(
            "tempra", "paracetamol", "tabletas", "500 mg", false, "LOTE-01", "2027-10-15", "10");

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal("El medicamento ya está registrado. Para agregar otro lote usa Entrada de lote.", ex.Message);
        Assert.Empty(medRepo.Guardados);
        Assert.False(uow.Confirmado);
    }

    [Fact]
    public async Task Sin_sesion_activa_lanza_no_autenticado()
    {
        var handler = new RegistrarMedicamentoHandler(
            new MedicamentoRepositoryFalso(),
            new LoteRepositoryFalso(),
            new MovimientoRepositoryFalso(),
            new UsuarioActualFalso(null),
            new UnitOfWorkFalso(),
            new RelojFalso());

        var command = new RegistrarMedicamentoCommand(
            "Tempra", "Paracetamol", "Tabletas", "500 mg", false, "LOTE-01", "2027-10-15", "10");

        await Assert.ThrowsAsync<NoAutenticadoException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void Validator_detecta_campos_vacios_fecha_inexistente_lote_invalido_y_cantidad_invalida()
    {
        var validator = new RegistrarMedicamentoValidator();
        var command = new RegistrarMedicamentoCommand("", "", "", "", false, "LOTE 1!", "2027-02-31", "-5");

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Este campo es obligatorio.");
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "Fecha de caducidad no válida.");
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "El número de lote solo puede tener letras, números y guiones.");
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == "La cantidad debe ser un número entero de 0 o más.");
    }

    // --- Clases falsas para pruebas sin MySQL ---

    private sealed class MedicamentoRepositoryFalso : IMedicamentoRepository
    {
        public bool SimularDuplicado { get; init; }
        public List<Medicamento> Guardados { get; } = [];

        public Task<bool> ExisteDuplicadoAsync(
            string nombreComercial, string nombreSustancia, string presentacion, string concentracion, CancellationToken cancellationToken)
            => Task.FromResult(SimularDuplicado);

        public Task<int> AgregarAsync(Medicamento medicamento, CancellationToken cancellationToken)
        {
            Guardados.Add(medicamento);
            return Task.FromResult(Guardados.Count);
        }
    }

    private sealed class LoteRepositoryFalso : ILoteRepository
    {
        public List<Lote> Guardados { get; } = [];

        public Task<int> AgregarAsync(Lote lote, CancellationToken cancellationToken)
        {
            Guardados.Add(lote);
            return Task.FromResult(Guardados.Count);
        }
    }

    private sealed class MovimientoRepositoryFalso : IMovimientoRepository
    {
        public List<Movimiento> Guardados { get; } = [];

        public Task<int> AgregarConDetallesAsync(Movimiento movimiento, CancellationToken cancellationToken)
        {
            Guardados.Add(movimiento);
            return Task.FromResult(Guardados.Count);
        }
    }

    private sealed class UnitOfWorkFalso : IUnitOfWork
    {
        public bool Confirmado { get; private set; }
        public Task GuardarCambiosAsync(CancellationToken cancellationToken)
        {
            Confirmado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RelojFalso : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class UsuarioActualFalso(int? idUsuario) : IUsuarioActual
    {
        public int? IdUsuario => idUsuario;
        public string NombreUsuario => "tester";
        public string NombreCompleto => "Usuario Prueba";
        public string Rol => "Empleado";
    }
}