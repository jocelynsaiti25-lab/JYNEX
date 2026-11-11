/** HU-03: debe coincidir con CrearUsuarioRequest del backend. idRol: 1 = Administrador, 2 = Empleado. */
export interface CrearUsuarioRequest {
  nombre: string;
  apellidos: string;
  nombreUsuario: string;
  correo: string;
  telefono: string;
  contrasenia: string;
  idRol: number;
}

/** HU-03: debe coincidir con CrearUsuarioResponse del backend (nunca trae la contraseña). */
export interface CrearUsuarioResponse {
  idUsuario: number;
  nombreUsuario: string;
  nombreCompleto: string;
  correo: string;
  rol: string;
}

/** HU-04: debe coincidir con UsuarioRegistradoResponse del backend (nunca trae la contraseña ni su hash). */
export interface UsuarioRegistrado {
  idUsuario: number;
  nombreCompleto: string;
  nombreUsuario: string;
  correo: string;
  telefono: string;
  rol: string;
}

/** Roles que se pueden asignar (mismos id que la tabla rol de MySQL). */
export const ROLES = [
  { id: 1, nombre: 'Administrador' },
  { id: 2, nombre: 'Empleado' },
] as const;
