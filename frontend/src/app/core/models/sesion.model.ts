/** Debe coincidir con IniciarSesionResponse del backend. */
export interface IniciarSesionResponse {
  idUsuario: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
}

/** Debe coincidir con IniciarSesionRequest del backend. */
export interface IniciarSesionRequest {
  nombreUsuario: string;
  contrasenia: string;
}
