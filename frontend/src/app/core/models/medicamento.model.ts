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

/** Contrato de respuesta de GET /api/medicamentos/{id} (HU-09): precarga del formulario de edición. */
export interface MedicamentoParaEditar {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
  /** Solo de referencia (bloqueado): HU-09 escenario 10. */
  existenciasTotales: number;
  /** Solo de referencia (bloqueado): HU-09 escenario 10. */
  semaforoCaducidad: 'Verde' | 'Amarillo' | 'Rojo' | 'SinStock';
}

/** Contrato de petición de PUT /api/medicamentos/{id} (HU-09). */
export interface EditarMedicamentoRequest {
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
}

/** Contrato de respuesta de PUT /api/medicamentos/{id} (HU-09). */
export interface EditarMedicamentoResponse {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
  seGuardaronCambios: boolean;
  mensaje: string;
}