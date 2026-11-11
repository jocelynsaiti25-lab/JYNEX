import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

/**
 * Política de contraseñas COMPARTIDA (HU-03 crear usuario y HU-05 reasignar contraseña).
 * Es la misma que el backend (Application/Common/Validacion/ReglasContrasenia.cs):
 * de 8 a 100 caracteres, con al menos una mayúscula, una minúscula y un número.
 *
 * Uso: contrasenia: ['', [Validators.required, ...validadoresContrasenia]]
 */
export const validadoresContrasenia: ValidatorFn[] = [
  Validators.minLength(8),
  Validators.maxLength(100),
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
  return Object.keys(errores).length ? errores : null;
}

/** Mensaje de la PRIMERA regla de contraseña que no se cumple (mismos textos y orden que el backend). */
export function mensajeContrasenia(errores: ValidationErrors): string {
  if ('minlength' in errores) return 'La contraseña debe tener al menos 8 caracteres.';
  if ('maxlength' in errores) return 'La contraseña no puede tener más de 100 caracteres.';
  if ('sinMayuscula' in errores) return 'La contraseña debe tener al menos una letra mayúscula.';
  if ('sinMinuscula' in errores) return 'La contraseña debe tener al menos una letra minúscula.';
  if ('sinNumero' in errores) return 'La contraseña debe tener al menos un número.';
  return 'La contraseña no es válida.';
}
