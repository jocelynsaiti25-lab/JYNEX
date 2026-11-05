import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';

/**
 * HU-01: si cualquier petición responde 401 (sesión expirada o inválida),
 * se limpia la sesión y se manda al inicio de sesión con el aviso "sesión expirada".
 * Las peticiones de autenticación se excluyen porque ellas mismas manejan el 401.
 */
export const sesionExpiradaInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const esPeticionDeAuth = req.url.includes('/auth/');
      if (error.status === 401 && !esPeticionDeAuth) {
        auth.limpiarSesion();
        router.navigate(['/login'], { queryParams: { expirada: 1 } });
      }
      return throwError(() => error);
    }),
  );
};
