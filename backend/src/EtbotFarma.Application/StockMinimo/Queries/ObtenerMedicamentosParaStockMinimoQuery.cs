using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.StockMinimo.Dtos;

namespace EtbotFarma.Application.StockMinimo.Queries;

/// <summary>
/// HU-11 escenario 1: lista los medicamentos del catálogo para la pantalla de control de stock
/// mínimo, con sus existencias vigentes y alertas ya calculadas. Solo LEE.
/// </summary>
public sealed record ObtenerMedicamentosParaStockMinimoQuery : IQuery<IReadOnlyList<MedicamentoStockDto>>;
