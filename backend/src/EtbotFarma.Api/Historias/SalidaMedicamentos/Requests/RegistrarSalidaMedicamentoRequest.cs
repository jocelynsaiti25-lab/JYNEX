
namespace EtbotFarma.Api.Historias.SalidaMedicamentos;

public sealed record RegistrarSalidaMedicamentoRequest(
    int? IdMedicamento,
    int? Cantidad);
