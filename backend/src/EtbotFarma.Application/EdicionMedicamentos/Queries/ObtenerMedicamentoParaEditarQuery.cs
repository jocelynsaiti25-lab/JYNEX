using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EdicionMedicamentos.Dtos;

namespace EtbotFarma.Application.EdicionMedicamentos.Queries;

/// <summary>
/// HU-09 escenario 2: datos del medicamento elegido en el inventario (HU-08) para precargar el formulario
/// de edición. Solo LEE. Handler: ObtenerMedicamentoParaEditarHandler
/// </summary>
public sealed record ObtenerMedicamentoParaEditarQuery(int IdMedicamento) : IQuery<MedicamentoParaEditarDto>;
