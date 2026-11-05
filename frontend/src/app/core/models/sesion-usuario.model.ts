/** Debe coincidir con SesionUsuarioResponse del backend. */
export interface SesionUsuario {
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
