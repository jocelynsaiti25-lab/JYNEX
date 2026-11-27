using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Messaging;

namespace EtbotFarma.Application.BitacoraMovimientos.Queries;

public sealed record ExportarBitacoraQuery(
    DateOnly? Desde,
    DateOnly? Hasta,
    int? IdUsuario,
    int? IdMedicamento,
    string? Tipo) : IQuery<ArchivoExportadoDto>;
