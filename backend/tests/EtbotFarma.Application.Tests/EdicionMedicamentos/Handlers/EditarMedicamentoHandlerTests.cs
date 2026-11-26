using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.EdicionMedicamentos.Commands;
using EtbotFarma.Application.EdicionMedicamentos.Handlers;
using EtbotFarma.Application.Tests.Falsos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.Tests.EdicionMedicamentos.Handlers;

/// <summary>Pruebas de HU-09: edición de datos descriptivos del medicamento.</summary>
public class EditarMedicamentoHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    private const int IdAdmin = 1;

    private readonly MedicamentoRepositoryFalso _medicamentos = new();
    private readonly AuditoriaRepositoryFalso _auditorias = new();
    private readonly UnitOfWorkFalso _unitOfWork = new();
    private readonly UsuarioActualFalso _usuarioActual = new() { IdUsuario = IdAdmin, NombreUsuario = "admin", Rol = "Administrador" };
    private readonly Medicamento _tempra;
    private readonly EditarMedicamentoHandler _handler;

    public EditarMedicamentoHandlerTests()
    {
        _tempra = Medicamento.Reconstruir(
            1, "Tempra", "Paracetamol", "Tabletas", "500 mg", esControlado: false, stockMinimo: 5, activo: true,
            Ahora.UtcDateTime);
        _medicamentos.Medicamentos.Add(_tempra);

        _handler = new EditarMedicamentoHandler(_medicamentos, _auditorias, _usuarioActual, _unitOfWork, new RelojFalso(Ahora));
    }

    private Task<EtbotFarma.Application.EdicionMedicamentos.Dtos.MedicamentoEditadoDto> EditarAsync(
        string nombreComercial = "Tempra Forte", string nombreSustancia = "Paracetamol",
        string presentacion = "Jarabe", string concentracion = "250 mg/5 ml", bool esControlado = true)
        => _handler.Handle(new EditarMedicamentoCommand(1, nombreComercial, nombreSustancia, presentacion, concentracion, esControlado), CancellationToken.None);

    /// <summary>Escenario 1: actualiza, registra auditoría y confirma en UNA transacción.</summary>
    [Fact]
    public async Task Edicion_exitosa_actualiza_y_registra_auditoria()
    {
        var resultado = await EditarAsync();

        Assert.True(resultado.SeGuardaronCambios);
        Assert.Equal("Tempra Forte", _tempra.NombreComercial);
        Assert.Same(_tempra, Assert.Single(_medicamentos.Actualizados));
        Assert.Equal(1, _unitOfWork.VecesGuardado);

        var registro = Assert.Single(_auditorias.Registros);
        Assert.Equal(IdAdmin, registro.IdUsuario);
        Assert.Equal("EDITAR MEDICAMENTO", registro.Accion);
        Assert.Equal("medicamento", registro.TablaAfectada);
        Assert.Equal(1, registro.IdRegistro);
        Assert.Contains("Tempra\"", registro.ValorAnterior);   // dato anterior
        Assert.Contains("Tempra Forte", registro.ValorNuevo);  // dato nuevo
    }

    /// <summary>Escenario 11: conserva el mismo Id (nunca se crea un medicamento nuevo).</summary>
    [Fact]
    public async Task Conserva_el_mismo_id_del_medicamento()
    {
        await EditarAsync();

        Assert.Equal(1, _tempra.Id);
    }

    /// <summary>Escenario 12: sin cambios reales no guarda nada ni audita.</summary>
    [Fact]
    public async Task Sin_cambios_reales_no_guarda_ni_audita()
    {
        var resultado = await EditarAsync("Tempra", "Paracetamol", "Tabletas", "500 mg", esControlado: false);

        Assert.False(resultado.SeGuardaronCambios);
        Assert.Equal("No se detectaron cambios por guardar", resultado.Mensaje);
        Assert.Empty(_medicamentos.Actualizados);
        Assert.Empty(_auditorias.Registros);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    /// <summary>Escenario 9: cambiar solo "Medicamento controlado" del mismo registro se guarda sin chocar.</summary>
    [Fact]
    public async Task Cambiar_solo_es_controlado_del_mismo_medicamento_se_guarda()
    {
        _medicamentos.Medicamentos.Add(Medicamento.Reconstruir(
            2, "Otro", "Ibuprofeno", "Tabletas", "200 mg", esControlado: false, stockMinimo: 0, activo: true, Ahora.UtcDateTime));

        var resultado = await EditarAsync("Tempra", "Paracetamol", "Tabletas", "500 mg", esControlado: true);

        Assert.True(resultado.SeGuardaronCambios);
        Assert.True(_tempra.EsControlado);
    }

    /// <summary>Escenario 8: la combinación ya existe en OTRO medicamento → 422 y nada se guarda.</summary>
    [Fact]
    public async Task Con_duplicado_en_otro_medicamento_no_cambia_nada()
    {
        _medicamentos.Medicamentos.Add(Medicamento.Reconstruir(
            2, "Dolex", "Paracetamol", "Tabletas", "500 mg", esControlado: false, stockMinimo: 0, activo: true, Ahora.UtcDateTime));
        var medicamentosConDuplicado = new MedicamentoRepositoryFalso { SimularDuplicadoAlEditar = true };
        medicamentosConDuplicado.Medicamentos.Add(_tempra);
        var handler = new EditarMedicamentoHandler(medicamentosConDuplicado, _auditorias, _usuarioActual, _unitOfWork, new RelojFalso(Ahora));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new EditarMedicamentoCommand(1, "Dolex", "Paracetamol", "Tabletas", "500 mg", false), CancellationToken.None));

        Assert.Equal("Ya existe otro medicamento registrado con esos datos en el catálogo.", ex.Message);
        Assert.Equal("Tempra", _tempra.NombreComercial);   // intacto
        Assert.Empty(_auditorias.Registros);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    /// <summary>Un id que no existe → 404 "El medicamento no existe" y nada se guarda.</summary>
    [Fact]
    public async Task Con_un_medicamento_que_no_existe_no_cambia_nada()
    {
        var ex = await Assert.ThrowsAsync<NoEncontradoException>(
            () => _handler.Handle(new EditarMedicamentoCommand(999, "X", "Y", "Z", "W", false), CancellationToken.None));

        Assert.Equal("El medicamento no existe", ex.Message);
        Assert.Empty(_medicamentos.Actualizados);
        Assert.Empty(_auditorias.Registros);
        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }

    /// <summary>Sin sesión (defensa adicional, aunque el controller ya exige Administrador).</summary>
    [Fact]
    public async Task Sin_sesion_lanza_NoAutenticadoException()
    {
        _usuarioActual.IdUsuario = null;

        await Assert.ThrowsAsync<NoAutenticadoException>(() => EditarAsync());

        Assert.Equal(0, _unitOfWork.VecesGuardado);
    }
}
