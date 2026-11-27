using System.Globalization;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EntradaLote.Commands;
using EtbotFarma.Application.EntradaLote.Dtos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.EntradaLote.Handlers;

/// <summary>
/// HU-07: Coordina la entrada de un nuevo lote, registra el movimiento ENTRADA en bitácora
/// y recalcula existencias y semáforo dentro de una única transacción atómica.
/// </summary>
public sealed class RegistrarEntradaLoteHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IMovimientoRepository movimientos,
    IUsuarioActual usuarioActual,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<RegistrarEntradaLoteCommand, EntradaLoteRegistradaDto>
{
    public async Task<EntradaLoteRegistradaDto> Handle(
        RegistrarEntradaLoteCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Verificar sesión activa
        if (usuarioActual.IdUsuario is not int idUsuario)
            throw new NoAutenticadoException();

        // 2. Verificar que el medicamento exista y esté activo (HU-07 escenarios 1 y 2)
        var medicamento = await medicamentos.ObtenerPorIdAsync(command.IdMedicamento, cancellationToken);
        if (medicamento is null)
            throw new DomainException("El medicamento seleccionado no existe en el catálogo.");

        medicamento.ValidarDisponibleParaEntrada();

        // 3. Verificar unicidad compuesta Medicamento + NumeroLote (HU-07 escenarios 6 y 7)
        var numeroLoteNormalizado = command.NumeroLote.Trim();
        var loteDuplicado = await lotes.ExisteNumeroLoteEnMedicamentoAsync(
            medicamento.Id,
            numeroLoteNormalizado,
            cancellationToken);

        if (loteDuplicado)
            throw new DomainException($"El número de lote '{numeroLoteNormalizado}' ya existe para este medicamento.");

        // 4. Crear entidades del Domain (valida caducidad > hoy y cantidad > 0) (HU-07 escenarios 4 y 5)
        var ahoraUtc = reloj.UtcNow.UtcDateTime;
        var hoy = reloj.Hoy;
        var fechaCaducidad = DateOnly.ParseExact(command.FechaCaducidad.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var cantidad = int.Parse(command.Cantidad.Trim(), CultureInfo.InvariantCulture);

        var nuevoLote = Lote.CrearEntrada(
            medicamento.Id,
            numeroLoteNormalizado,
            fechaCaducidad,
            cantidad,
            hoy);

        var idLote = await lotes.AgregarAsync(nuevoLote, cancellationToken);
        nuevoLote.AsignarId(idLote);

        // 5. Registrar ENTRADA en la bitácora con usuario, fecha y hora (HU-07 escenario 10)
        var movimiento = Movimiento.CrearEntradaLote(
            medicamento.Id,
            idLote,
            idUsuario,
            cantidad,
            ahoraUtc);

        await movimientos.AgregarConDetallesAsync(movimiento, cancellationToken);

        var lotesActualizados = await lotes.ObtenerPorMedicamentosAsync([medicamento.Id], cancellationToken);
        var existencias = medicamento.CalcularExistencias(lotesActualizados, hoy);

        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        var descripcionMedicamento = $"{medicamento.NombreComercial} ({medicamento.NombreSustancia} {medicamento.Concentracion} - {medicamento.Presentacion})";

        return new EntradaLoteRegistradaDto(
            nuevoLote.Id,
            medicamento.Id,
            descripcionMedicamento,
            nuevoLote.NumeroLote,
            nuevoLote.FechaCaducidad,
            cantidad,
            nuevoLote.ObtenerSemaforo(hoy).ToString(),
            existencias.Total,
            existencias.SemaforoGeneral,
            existencias.EstaAgotado,
            existencias.EnStockBajo,
            "Entrada de lote registrada correctamente.");
    }
}
