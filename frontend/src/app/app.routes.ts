import { Routes } from '@angular/router';

import { sesionGuard, invitadoGuard } from './core/guards/sesion.guard';

/**
 * Rutas de la aplicación. Cada pantalla se carga solo cuando se necesita (lazy loading).
 *   login  → pública (si ya hay sesión, manda al inicio)
 *   inicio → protegida (HU-01 criterio 5)
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'inicio' },
  {
    path: 'iniciar-sesion',
    canActivate: [invitadoGuard],
    loadComponent: () =>
      import('./features/inicio-sesion/iniciar-sesion.component').then((m) => m.IniciarSesionComponent),
  },
  {
    path: 'inicio',
    canActivate: [sesionGuard],
    loadComponent: () =>
      import('./features/inicio/inicio.component').then((m) => m.InicioComponent),
  },
  { path: '**', redirectTo: 'inicio' },
];
