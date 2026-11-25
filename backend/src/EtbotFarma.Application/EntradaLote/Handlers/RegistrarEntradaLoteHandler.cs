using System.Globalization;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EntradaLote.Commands;
using EtbotFarma.Application.EntradaLote.Dtos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;
using EtbotFarma.Domain.Enums;

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
        var hoy = DateOnly.FromDateTime(ahoraUtc);
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

        // 6. Confirmar toda la transacción atómica (HU-07 escenario 13: rollback automático si algo falla)
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        // 7. Recalcular existencias totales, semáforo y estados agotado / stock bajo (HU-07 escenarios 11 y 12)
        var lotesActualizados = await lotes.ObtenerPorMedicamentoAsync(medicamento.Id, cancellationToken);
        var lotesConStock = lotesActualizados.Where(l => l.Stock > 0).ToList();
        if (lotesConStock.All(l => l.Id != nuevoLote.Id))
            lotesConStock.Add(nuevoLote);

        var existenciasTotales = lotesConStock.Sum(l => l.Stock);
        var semaforoLote = nuevoLote.ObtenerSemaforo(hoy).ToString();
        var semaforoGeneral = CalcularSemaforoGeneral(lotesConStock.Select(l => l.ObtenerSemaforo(hoy)));
        var estaAgotado = existenciasTotales == 0;
        var enStockBajo = !estaAgotado && medicamento.StockMinimo > 0 && existenciasTotales <= medicamento.StockMinimo;

        var descripcionMedicamento = $"{medicamento.NombreComercial} ({medicamento.NombreSustancia} {medicamento.Concentracion} - {medicamento.Presentacion})";

        return new EntradaLoteRegistradaDto(
            nuevoLote.Id,
            medicamento.Id,
            descripcionMedicamento,
            nuevoLote.NumeroLote,
            nuevoLote.FechaCaducidad,
            cantidad,
            semaforoLote,
            existenciasTotales,
            semaforoGeneral,
            estaAgotado,
            enStockBajo,
            "Entrada de lote registrada correctamente.");
    }

    private static string CalcularSemaforoGeneral(IEnumerable<SemaforoCaducidad> semaforos)
    {
        var lista = semaforos.ToList();
        if (lista.Count == 0) return "SinStock";
        if (lista.Contains(SemaforoCaducidad.Rojo)) return SemaforoCaducidad.Rojo.ToString();
        if (lista.Contains(SemaforoCaducidad.Amarillo)) return SemaforoCaducidad.Amarillo.ToString();
        return SemaforoCaducidad.Verde.ToString();
    }
}