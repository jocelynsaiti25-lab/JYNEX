import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of } from 'rxjs';

import { Rol } from '../layout/menu';
import { SesionService } from '../services/sesion.service';

/**
 * Solo deja entrar a la pantalla si el usuario tiene uno de los roles indicados.
 * Si no, lo regresa al inicio (HU-03 escenario 4: un Empleado escribe la dirección a mano).
 *
 *   { path: 'usuarios/crear', canActivate: [rolGuard(['Administrador'])], ... }
 *
 * Ocultar la pantalla NO es seguridad: la API también revisa el rol ([Authorize(Roles = ...)]).
 */
export const rolGuard = (roles: Rol[]): CanActivateFn => () => {
  const sesion = inject(SesionService);
  const router = inject(Router);

  const revisar = () => {
    const rol = sesion.usuario()?.rol as Rol | undefined;
    return rol && roles.includes(rol) ? true : router.createUrlTree(['/inicio']);
  };

  // Si todavía no se conoce al usuario (recién se recargó la página), se pregunta a la API
  return sesion.usuario() ? of(revisar()) : sesion.verificarSesion().pipe(map(() => revisar()));
};
