import { Routes } from '@angular/router';

import { authGuard, invitadoGuard } from './core/guards/auth.guard';

/**
 * Rutas de la aplicación. Cada pantalla se carga solo cuando se necesita (lazy loading).
 *   login  → pública (si ya hay sesión, manda al inicio)
 *   inicio → protegida (HU-01 criterio 5)
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'inicio' },
  {
    path: 'login',
    canActivate: [invitadoGuard],
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'inicio',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/inicio/inicio.component').then((m) => m.InicioComponent),
  },
  { path: '**', redirectTo: 'inicio' },
];
