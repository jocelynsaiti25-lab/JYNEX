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
  const teniaSesion = auth.usuario() !== null;

  return auth.verificarSesion().pipe(
    map((sesionValida) => {
      if (sesionValida) {
        return true;
      }
      return router.createUrlTree(['/login'], {
        queryParams: teniaSesion ? { expirada: 1 } : {},
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
