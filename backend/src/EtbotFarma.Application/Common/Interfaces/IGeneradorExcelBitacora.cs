using EtbotFarma.Application.BitacoraMovimientos.Dtos;

namespace EtbotFarma.Application.Common.Interfaces;

public interface IGeneradorExcelBitacora
{
    byte[] Generar(IReadOnlyList<MovimientoBitacoraDto> movimientos);
}
