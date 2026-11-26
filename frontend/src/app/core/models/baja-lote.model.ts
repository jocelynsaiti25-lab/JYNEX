export interface DarDeBajaLoteRequest {
  cantidadConsultada: number;
  motivo: string;
}

export interface LoteDadoDeBaja {
  idLote: number;
  idMedicamento: number;
  nombreMedicamento: string;
  numeroLote: string;
  fechaCaducidad: string;
  cantidadRetirada: number;
  motivo: string;
  mensaje: string;
}

export interface ResultadoBaja {
  tipo: 'exito' | 'error';
  mensaje: string;
}
