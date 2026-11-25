using System.Globalization;
using EtbotFarma.Application.RegistroMedicamentos.Commands;
using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.RegistroMedicamentos.Dtos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.RegistroMedicamentos.Handlers;

/// <summary>
/// HU-06: Coordina el alta de un medicamento nuevo, su primer lote y (si cantidad > 0)
/// su movimiento de ENTRADA en la bitácora dentro de una sola transacción.
/// </summary>
public sealed class RegistrarMedicamentoHandler(
    IMedicamentoRepository medicamentos,
    ILoteRepository lotes,
    IMovimientoRepository movimientos,
    IUsuarioActual usuarioActual,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<RegistrarMedicamentoCommand, MedicamentoRegistradoDto>
{
    public async Task<MedicamentoRegistradoDto> Handle(
        RegistrarMedicamentoCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Verificar sesión activa (HU-06 escenario 7)
        if (usuarioActual.IdUsuario is not int idUsuario)
        {
            throw new NoAutenticadoException();
        }

        // 2. Verificar que el medicamento no esté duplicado en el catálogo (HU-06 escenarios 14 y 15)
        var existeDuplicado = await medicamentos.ExisteDuplicadoAsync(
            command.NombreComercial.Trim(),
            command.NombreSustancia.Trim(),
            command.Presentacion.Trim(),
            command.Concentracion.Trim(),
            cancellationToken);

        if (existeDuplicado)
        {
            throw new DomainException(
                "El medicamento ya está registrado. Para agregar otro lote usa Entrada de lote.");
        }

        // 3. Crear entidades del Domain (aquí se validan caducidad, lote y cantidad)
        var ahoraUtc = reloj.UtcNow.UtcDateTime;
        var hoy = DateOnly.FromDateTime(ahoraUtc);
        var fechaCaducidad = DateOnly.ParseExact(command.FechaCaducidad.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var cantidadInicial = int.Parse(command.CantidadInicial.Trim(), CultureInfo.InvariantCulture);

        var medicamento = Medicamento.Crear(
            command.NombreComercial,
            command.NombreSustancia,
            command.Presentacion,
            command.Concentracion,
            command.EsControlado,
            ahoraUtc);

        var idMedicamento = await medicamentos.AgregarAsync(medicamento, cancellationToken);
        medicamento.AsignarId(idMedicamento);

        var lote = Lote.Crear(
            idMedicamento,
            command.NumeroLote,
            fechaCaducidad,
            cantidadInicial,
            hoy);

        var idLote = await lotes.AgregarAsync(lote, cancellationToken);
        lote.AsignarId(idLote);

        // 4. Registrar ENTRADA en la bitácora SOLO si cantidad > 0 (HU-06 escenarios 1, 2 y 3)
        var seRegistroEntrada = cantidadInicial > 0;
        if (seRegistroEntrada)
        {
            var movimiento = Movimiento.CrearEntradaInicial(
                idMedicamento,
                idLote,
                idUsuario,
                cantidadInicial,
                ahoraUtc);

            await movimientos.AgregarConDetallesAsync(movimiento, cancellationToken);
        }

        // 5. Confirmar toda la transacción (HU-06 escenario 17: si algo falla antes, hay ROLLBACK)
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        var semaforo = lote.ObtenerSemaforo(hoy).ToString();

        return new MedicamentoRegistradoDto(
            medicamento.Id,
            medicamento.NombreComercial,
            medicamento.NombreSustancia,
            medicamento.Presentacion,
            medicamento.Concentracion,
            medicamento.EsControlado,
            lote.Id,
            lote.NumeroLote,
            lote.FechaCaducidad,
            lote.Stock,
            semaforo,
            seRegistroEntrada,
            "Medicamento registrado");
    }
}

