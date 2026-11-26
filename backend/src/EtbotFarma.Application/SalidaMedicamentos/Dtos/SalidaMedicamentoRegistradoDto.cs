
namespace EtbotFarma.Application.SalidaMedicamentos.Dtos;

/// <summary>
/// HU-13: resultado tras confirmar la salida de medicamentos.
/// </summary>
public sealed record SalidaMedicamentoRegistradoDto(
    int IdMovimiento,
    int IdMedicamento,
    string NombreMedicamento,
    int CantidadRetirada,
    IReadOnlyList<LoteSalidaDto> LotesUtilizados,
    int ExistenciasTotalesActualizadas,
    string SemaforoGeneralActualizado,
    bool EstaAgotado,
    bool EnStockBajo,
    string Mensaje);
