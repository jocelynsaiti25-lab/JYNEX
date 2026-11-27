export interface LoteVigente {
  idLote: number;
  numeroLote: string;
  fechaCaducidad: string;
  existencias: number;
  semaforo: string;
}

export interface MedicamentoParaSalida {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
  existenciasVigentes: number;
  estaAgotado: boolean;
  lotes: LoteVigente[];
}

export interface LotePropuesto {
  idLote: number;
  numeroLote: string;
  fechaCaducidad: string;
  cantidad: number;
}

export interface PropuestaSalida {
  idMedicamento: number;
  medicamento: string;
  cantidadTotal: number;
  existenciasVigentes: number;
  lotes: LotePropuesto[];
}

export interface RegistrarSalidaRequest {
  idMedicamento: number;
  cantidad: string;
}

export interface SalidaRegistrada {
  idMovimiento: number;
  idMedicamento: number;
  medicamento: string;
  cantidadTotal: number;
  lotes: LotePropuesto[];
  mensaje: string;
}
