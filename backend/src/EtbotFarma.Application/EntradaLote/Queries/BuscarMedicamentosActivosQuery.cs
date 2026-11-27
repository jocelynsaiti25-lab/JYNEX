using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EntradaLote.Dtos;

namespace EtbotFarma.Application.EntradaLote.Queries;

/// <summary>
/// HU-07 escenarios 1 y 2: consulta medicamentos activos del catálogo por nombre, sustancia o lote.
/// </summary>
public sealed record BuscarMedicamentosActivosQuery(string? Termino)
    : IQuery<IReadOnlyList<MedicamentoActivoDto>>;