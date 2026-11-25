export interface ItemInventario {
  idMedicamento: number;
  idLote: number | null;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
  esControlado: boolean;
  stockMinimo: number;
  numeroLote: string;
  existencias: number;
  fechaCaducidad: string | null;
  semaforoCaducidad: 'Verde' | 'Amarillo' | 'Rojo' | 'SinStock';
  estadoDisponibilidad: 'Disponible' | 'Stock bajo' | 'Agotado';
}