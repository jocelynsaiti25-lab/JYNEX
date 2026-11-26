
namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record RegistrarSalidaMedicamentoResponse(
    int IdMovimiento,
    int IdMedicamento,
    string NombreMedicamento,
    int CantidadRetirada,
    IReadOnlyList<LoteSalidaResponse> LotesUtilizados,
    int ExistenciasTotalesActualizadas,
    string SemaforoGeneralActualizado,
    bool EstaAgotado,
    bool EnStockBajo,
    string Mensaje);
