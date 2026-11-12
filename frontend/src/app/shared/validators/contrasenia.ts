import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

/**
 * Política de contraseñas COMPARTIDA (HU-03 crear usuario y HU-05 reasignar contraseña).
 * Es la misma que el backend (Application/Common/Validacion/ReglasContrasenia.cs):
 * de 8 a 15 caracteres, con al menos una mayúscula, una minúscula, un número
 * y un carácter especial (por ejemplo @, #, $, %, ! o &).
 *
 * Uso: contrasenia: ['', [Validators.required, ...validadoresContrasenia]]
 */
export const CONTRASENIA_MINIMO = 8;
export const CONTRASENIA_MAXIMO = 15;

/** Texto de ayuda que se muestra debajo del campo (mismo en HU-03 y HU-05). */
export const AYUDA_CONTRASENIA =
  `De ${CONTRASENIA_MINIMO} a ${CONTRASENIA_MAXIMO} caracteres, con mayúscula, minúscula, número y un carácter especial (@ # $ % ! &).`;

/** Carácter especial = cualquier cosa que no sea letra, número ni espacio (igual que el backend). */
const CARACTER_ESPECIAL = /[^\p{L}\p{N}\s]/u;

export const validadoresContrasenia: ValidatorFn[] = [
  Validators.minLength(CONTRASENIA_MINIMO),
  Validators.maxLength(CONTRASENIA_MAXIMO),
  reglasContrasenia,
];

/** Reglas que no trae Angular. Devuelve un error distinto por regla para decir exactamente qué falta. */
export function reglasContrasenia(control: AbstractControl<string>): ValidationErrors | null {
  const valor = control.value ?? '';
  if (!valor) {
    return null;   // de "obligatoria" se encarga Validators.required
  }
  const errores: ValidationErrors = {};
  if (!/[A-Z]/.test(valor)) errores['sinMayuscula'] = true;
  if (!/[a-z]/.test(valor)) errores['sinMinuscula'] = true;
  if (!/[0-9]/.test(valor)) errores['sinNumero'] = true;
  if (!CARACTER_ESPECIAL.test(valor)) errores['sinEspecial'] = true;
  return Object.keys(errores).length ? errores : null;
}

/** Mensaje de la PRIMERA regla de contraseña que no se cumple (mismos textos y orden que el backend). */
export function mensajeContrasenia(errores: ValidationErrors): string {
  if ('minlength' in errores) return `La contraseña debe tener al menos ${CONTRASENIA_MINIMO} caracteres.`;
  if ('maxlength' in errores) return `La contraseña no puede tener más de ${CONTRASENIA_MAXIMO} caracteres.`;
  if ('sinMayuscula' in errores) return 'La contraseña debe tener al menos una letra mayúscula.';
  if ('sinMinuscula' in errores) return 'La contraseña debe tener al menos una letra minúscula.';
  if ('sinNumero' in errores) return 'La contraseña debe tener al menos un número.';
  if ('sinEspecial' in errores) return 'La contraseña debe tener al menos un carácter especial (por ejemplo @, #, $, %, ! o &).';
  return 'La contraseña no es válida.';
}
