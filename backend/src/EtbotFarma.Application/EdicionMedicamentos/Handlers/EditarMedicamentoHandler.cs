using EtbotFarma.Application.Common.Exceptions;
using EtbotFarma.Application.Common.Interfaces;
using EtbotFarma.Application.Common.Interfaces.Repositorios;
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EdicionMedicamentos.Commands;
using EtbotFarma.Application.EdicionMedicamentos.Dtos;
using EtbotFarma.Domain.Common;
using EtbotFarma.Domain.Entities;

namespace EtbotFarma.Application.EdicionMedicamentos.Handlers;

/// <summary>
/// HU-09: edita los datos descriptivos de un medicamento ya registrado.
/// Solo el Administrador llega aquí: el controller tiene [Authorize(Roles = "Administrador")] (escenario 3).
/// Los datos YA llegan validados: MediatR ejecutó EditarMedicamentoValidator antes (escenario 7).
///
/// Regla de negocio: actualización + auditoría se guardan en UNA sola transacción (un solo COMMIT).
/// Si algo falla antes, no queda ningún cambio a medias (escenario 15).
/// </summary>
public sealed class EditarMedicamentoHandler(
    IMedicamentoRepository medicamentos,
    IAuditoriaRepository auditorias,
    IUsuarioActual usuarioActual,
    IUnitOfWork unitOfWork,
    IDateTimeProvider reloj) : ICommandHandler<EditarMedicamentoCommand, MedicamentoEditadoDto>
{
    public async Task<MedicamentoEditadoDto> Handle(EditarMedicamentoCommand command, CancellationToken cancellationToken)
    {
        // 0. Quién hace el cambio: el administrador con sesión
        var idAdministrador = usuarioActual.IdUsuario ?? throw new NoAutenticadoException();

        // 1. El medicamento debe existir
        var medicamento = await medicamentos.ObtenerPorIdAsync(command.IdMedicamento, cancellationToken)
            ?? throw new NoEncontradoException("El medicamento no existe");

        // 2. Escenario 12: si no hay ningún cambio real, no se guarda nada ni se audita
        var hayCambios = medicamento.TieneCambiosDescriptivos(
            command.NombreComercial, command.NombreSustancia, command.Presentacion, command.Concentracion, command.EsControlado);

        if (!hayCambios)
        {
            return new MedicamentoEditadoDto(
                medicamento.Id, medicamento.NombreComercial, medicamento.NombreSustancia, medicamento.Presentacion,
                medicamento.Concentracion, medicamento.EsControlado, SeGuardaronCambios: false,
                "No se detectaron cambios por guardar");
        }

        // 3. Escenarios 8 y 9: la combinación no puede chocar con OTRO medicamento (el propio se excluye)
        var existeDuplicado = await medicamentos.ExisteDuplicadoAlEditarAsync(
            medicamento.Id, command.NombreComercial, command.NombreSustancia, command.Presentacion, command.Concentracion,
            cancellationToken);

        if (existeDuplicado)
        {
            throw new DomainException("Ya existe otro medicamento registrado con esos datos en el catálogo.");
        }

        // 4. Aplicar la edición (reglas de negocio en el Domain) y guardar auditoría antes/después
        var valorAnterior = medicamento.DatosParaAuditoria();

        medicamento.Editar(
            command.NombreComercial, command.NombreSustancia, command.Presentacion, command.Concentracion, command.EsControlado);

        // Escenario 11: conserva su mismo Id; ActualizarAsync NUNCA toca stock, lotes ni movimientos
        await medicamentos.ActualizarAsync(medicamento, cancellationToken);

        var auditoria = Auditoria.Crear(
            idAdministrador,
            Auditoria.EditarMedicamento,
            Auditoria.TablaMedicamento,
            medicamento.Id,
            valorAnterior,
            medicamento.DatosParaAuditoria(),
            reloj.UtcNow.UtcDateTime);
        await auditorias.AgregarAsync(auditoria, cancellationToken);

        // 5. COMMIT una sola vez: actualización + auditoría juntos
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return new MedicamentoEditadoDto(
            medicamento.Id, medicamento.NombreComercial, medicamento.NombreSustancia, medicamento.Presentacion,
            medicamento.Concentracion, medicamento.EsControlado, SeGuardaronCambios: true,
            "Medicamento actualizado");
    }
}
