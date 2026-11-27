/** HU-15: detalle de lote asociado a un movimiento de inventario. */
export interface DetalleBitacora {
  idLote: number;
  numeroLote: string;
  cantidad: number;
}

/** HU-15: movimiento completo con los datos descriptivos actuales del usuario, medicamento y lotes. */
export interface MovimientoBitacora {
  idMovimiento: number;
  fecha: string;
  usuario: string;
  tipo: 'ENTRADA' | 'SALIDA' | 'BAJA';
  idMedicamento: number;
  medicamento: string;
  cantidadTotal: number;
  motivo: string | null;
  detalles: DetalleBitacora[];
}

/** HU-15: respuesta paginada del endpoint de consulta. */
export interface PaginaBitacora {
  movimientos: MovimientoBitacora[];
  pagina: number;
  tamanoPagina: number;
  totalMovimientos: number;
  totalPaginas: number;
}

/** Medicamento con historial, activo o inactivo, ofrecido como filtro de la bitácora. */
export interface MedicamentoBitacora {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
}

/** HU-15: filtros admitidos por GET api/bitacora/movimientos. */
export interface FiltrosBitacora {
  desde?: string;
  hasta?: string;
  idUsuario?: number;
  idMedicamento?: number;
  tipo?: MovimientoBitacora['tipo'];
  pagina: number;
  tamanoPagina: number;
}
