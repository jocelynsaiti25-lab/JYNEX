export interface MedicamentoActivo {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
  stockMinimo: number;
  existenciasTotales: number;
  semaforoGeneral: 'Verde' | 'Amarillo' | 'Rojo' | 'SinStock';
  estaAgotado: boolean;
  enStockBajo: boolean;
}

export interface RegistrarEntradaLoteRequest {
  idMedicamento: number;
  numeroLote: string;
  fechaCaducidad: string;
  cantidad: string;
}

export interface RegistrarEntradaLoteResponse {
  idLote: number;
  idMedicamento: number;
  nombreMedicamento: string;
  numeroLote: string;
  fechaCaducidad: string;
  cantidadIngresada: number;
  semaforoLote: 'Verde' | 'Amarillo' | 'Rojo';
  existenciasTotalesActualizadas: number;
  semaforoGeneralActualizado: 'Verde' | 'Amarillo' | 'Rojo' | 'SinStock';
  estaAgotado: boolean;
  enStockBajo: boolean;
  mensaje: string;
}