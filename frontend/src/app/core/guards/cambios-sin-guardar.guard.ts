import { CanDeactivateFn } from '@angular/router';

/**
 * Interfaz que implementan los componentes con formularios que deben advertir
 * antes de salir si hay datos capturados sin guardar (HU-06 escenario 19).
 */
export interface TieneCambiosSinGuardar {
  tieneCambiosSinGuardar(): boolean;
}

/**
 * Guard reutilizable de Angular: si el formulario tiene cambios sin guardar,
 * muestra confirmación antes de navegar a otra pantalla o cerrar sesión.
 */
export const cambiosSinGuardarGuard: CanDeactivateFn<TieneCambiosSinGuardar> = (componente) => {
  if (!componente.tieneCambiosSinGuardar()) {
    return true;
  }

  return window.confirm(
    'Tienes datos capturados sin guardar. Si sales ahora, se perderán. ¿Deseas salir de todos modos?'
  );
};