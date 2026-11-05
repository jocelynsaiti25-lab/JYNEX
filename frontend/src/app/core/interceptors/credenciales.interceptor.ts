import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Agrega withCredentials a todas las peticiones para que el navegador
 * envíe la cookie de sesión al servidor (necesario desde HU-01).
 */
export const credencialesInterceptor: HttpInterceptorFn = (req, next) =>
  next(req.clone({ withCredentials: true }));
