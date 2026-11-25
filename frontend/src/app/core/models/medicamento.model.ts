/** Contrato de petición para registrar un medicamento con su lote inicial (HU-06). */
export interface RegistrarMedicamentoRequest {
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
  numeroLote: string;
  fechaCaducidad: string;
  cantidadInicial: string;
}

/** Contrato de respuesta de POST /api/medicamentos. */
export interface RegistrarMedicamentoResponse {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
  idLote: number;
  numeroLote: string;
  fechaCaducidad: string;
  stock: number;
  semaforoCaducidad: 'Verde' | 'Amarillo' | 'Rojo';
  seRegistroEntrada: boolean;
  mensaje: string;
}