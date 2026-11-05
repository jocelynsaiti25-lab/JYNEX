import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';

import { AuthService } from '../services/auth.service';

/**
 * HU-01 criterio 5: solo deja entrar a las pantallas protegidas si hay sesión válida.
 * Si no la hay, manda al inicio de sesión. Si antes había sesión, avisa que expiró.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const habiaSesion = auth.habiaSesion();

  return auth.verificarSesion().pipe(
    map((sesionValida) => {
      if (sesionValida) {
        return true;
      }
      auth.limpiarSesion();
      return router.createUrlTree(['/login'], {
        queryParams: habiaSesion ? { expirada: 1 } : {},
      });
    }),
  );
};

/** Si el usuario ya tiene sesión, no tiene sentido mostrarle el login: va directo al inicio. */
export const invitadoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth
    .verificarSesion()
    .pipe(map((sesionValida) => (sesionValida ? router.createUrlTree(['/inicio']) : true)));
};
