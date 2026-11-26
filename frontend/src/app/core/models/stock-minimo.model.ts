/** HU-11: medicamento con su stock mínimo, existencias vigentes y alertas ya calculadas. */
export interface MedicamentoStock {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  stockMinimo: number;
  existenciasVigentes: number;
  existenciasCaducadas: number;
  tieneLotesCaducados: boolean;
  enStockBajo: boolean;
  estaAgotado: boolean;
}

/** HU-11 escenario 1: contrato de entrada para guardar el nuevo umbral. */
export interface ActualizarStockMinimoRequest {
  nuevoStockMinimo: number;
}

/** HU-11 escenario 6: resultado de guardar el nuevo stock mínimo. */
export interface ActualizarStockMinimoResponse {
  idMedicamento: number;
  stockMinimo: number;
  mensaje: string;
}
