
using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.SalidaMedicamentos.Dtos;

namespace EtbotFarma.Application.SalidaMedicamentos.Queries;

/// <summary>
/// Consulta los lotes disponibles para proponer una salida de medicamentos.
/// </summary>
public sealed record ProponerSalidaMedicamentoQuery(
    int IdMedicamento,
    int CantidadSolicitada)
    : IQuery<PropuestaSalidaDto>;
