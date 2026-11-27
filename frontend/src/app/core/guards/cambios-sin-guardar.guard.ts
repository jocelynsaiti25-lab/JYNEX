import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

/** Pantallas con formulario que avisan antes de salir si hay datos sin guardar. */
export interface ConCambiosSinGuardar {
  /** true = puede salir; o un Observable que responde cuando el usuario elige en la confirmación. */
  puedeSalir(): boolean | Observable<boolean>;
}

/**
 * HU-03 escenario 16 (y HU-02 criterio 4): si hay datos sin guardar, se pregunta antes de salir.
 *   { path: '...', canDeactivate: [cambiosSinGuardarGuard], ... }
 */
export const cambiosSinGuardarGuard: CanDeactivateFn<ConCambiosSinGuardar> = (pantalla) => pantalla.puedeSalir();
