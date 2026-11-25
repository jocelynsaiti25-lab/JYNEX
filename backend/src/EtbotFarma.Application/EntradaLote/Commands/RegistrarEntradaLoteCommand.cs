using EtbotFarma.Application.Common.Messaging;
using EtbotFarma.Application.EntradaLote.Dtos;

namespace EtbotFarma.Application.EntradaLote.Commands;

/// <summary>
/// HU-07: Comando para registrar la entrada de un nuevo lote a un medicamento existente y activo.
/// </summary>
public sealed record RegistrarEntradaLoteCommand(
    int IdMedicamento,
    string NumeroLote,
    string FechaCaducidad,
    string Cantidad) : ICommand<EntradaLoteRegistradaDto>;