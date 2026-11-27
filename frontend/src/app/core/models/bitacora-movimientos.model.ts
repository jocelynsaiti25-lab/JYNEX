export type TipoMovimiento = 'ENTRADA' | 'SALIDA' | 'BAJA';

export const TIPOS_MOVIMIENTO: readonly TipoMovimiento[] = ['ENTRADA', 'SALIDA', 'BAJA'];

export interface DetalleBitacora {
  idLote: number;
  numeroLote: string;
  cantidad: number;
}

export interface MovimientoBitacora {
  idMovimiento: number;
  fecha: string;
  idUsuario: number;
  usuario: string;
  tipo: TipoMovimiento;
  idMedicamento: number;
  medicamento: string;
  presentacion: string;
  concentracion: string;
  cantidadTotal: number;
  motivo: string | null;
  lotes: DetalleBitacora[];
}

export interface PaginaBitacora {
  movimientos: MovimientoBitacora[];
  pagina: number;
  tamanoPagina: number;
  total: number;
  totalPaginas: number;
}

export interface MedicamentoBitacora {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
}

export interface FiltroBitacora {
  desde?: string;
  hasta?: string;
  idUsuario?: number;
  idMedicamento?: number;
  tipo?: TipoMovimiento;
}
