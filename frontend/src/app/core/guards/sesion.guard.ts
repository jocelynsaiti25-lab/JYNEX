import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { SesionService } from '../services/sesion.service';

/**
 * HU-01 criterio 5: solo deja entrar a las pantallas protegidas si hay sesión válida.
 * Si no la hay, manda al inicio de sesión. Si antes había sesión, avisa que expiró.
 */
export const sesionGuard: CanActivateFn = () => {
  const sesion = inject(SesionService);
  const router = inject(Router);
  const habiaSesion = sesion.habiaSesion();

  return sesion.verificarSesion().pipe(
    map((sesionValida) => {
      if (sesionValida) {
        return true;
      }
      sesion.limpiarSesion();
      return router.createUrlTree(['/iniciar-sesion'], {
        queryParams: habiaSesion ? { expirada: 1 } : {},
      });
    }),
  );
};

/** Si el usuario ya tiene sesión, no tiene sentido mostrarle el login: va directo al inicio. */
export const invitadoGuard: CanActivateFn = () => {
  const sesion = inject(SesionService);
  const router = inject(Router);

  // HU-02 escenarios 11 y 13: si un cierre de sesión anterior no llegó al servidor,
  // se reintenta ANTES de revisar la sesión. Así nadie entra con la cuenta de quien salió.
  // Funcione o no el reintento, se muestra el inicio de sesión (nunca la cuenta anterior).
  if (sesion.hayCierrePendiente()) {
    return sesion.cerrarSesion().pipe(
      map(() => true),
      catchError(() => of(true)),
    );
  }

  return sesion
    .verificarSesion()
    .pipe(map((sesionValida) => (sesionValida ? router.createUrlTree(['/inicio']) : true)));
};
