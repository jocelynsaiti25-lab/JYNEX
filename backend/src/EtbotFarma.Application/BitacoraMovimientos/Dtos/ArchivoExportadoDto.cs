namespace EtbotFarma.Application.BitacoraMovimientos.Dtos;

public sealed record ArchivoExportadoDto(byte[] Contenido, string NombreArchivo, int TotalMovimientos);
