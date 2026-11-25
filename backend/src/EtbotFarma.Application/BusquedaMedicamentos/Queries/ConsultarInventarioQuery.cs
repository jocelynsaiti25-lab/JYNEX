using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.BusquedaMedicamentos.Dtos;

namespace EtbotFarma.Application.BusquedaMedicamentos.Queries;

/// <summary>
/// HU-08 escenarios 1 al 7: consulta el inventario filtrando por término (nombre comercial,
/// sustancia activa o número de lote) y/o por semáforo de caducidad (Rojo, Amarillo, Verde).
/// </summary>
public sealed record ConsultarInventarioQuery(
    string? Termino,
    string? Semaforo) : IQuery<IReadOnlyList<ItemInventarioDto>>;