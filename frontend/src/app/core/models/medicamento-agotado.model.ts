/** HU-12: medicamento activo sin existencias vigentes (agotado). */
export interface MedicamentoAgotado {
  idMedicamento: number;
  nombreComercial: string;
  nombreSustancia: string;
  presentacion: string;
  concentracion: string;
}
