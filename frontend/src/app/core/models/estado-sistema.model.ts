/** Debe coincidir con EstadoSistemaDto del backend. */
export interface EstadoSistema {
  aplicacion: string;
  version: string;
  fechaServidorUtc: string;
}
