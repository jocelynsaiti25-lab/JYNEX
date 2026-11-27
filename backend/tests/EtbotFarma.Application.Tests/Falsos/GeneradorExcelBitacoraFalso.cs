using EtbotFarma.Application.BitacoraMovimientos.Dtos;
using EtbotFarma.Application.Common.Interfaces;

namespace EtbotFarma.Application.Tests.Falsos;

public sealed class GeneradorExcelBitacoraFalso : IGeneradorExcelBitacora
{
    public List<MovimientoBitacoraDto> Recibidos { get; } = [];

    public int Llamadas { get; private set; }

    public byte[] Generar(IReadOnlyList<MovimientoBitacoraDto> movimientos)
    {
        Llamadas++;
        Recibidos.AddRange(movimientos);
        return [1, 2, 3];
    }
}
