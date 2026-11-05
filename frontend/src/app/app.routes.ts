import { Routes } from '@angular/router';

/**
 * Rutas de la aplicación. Cada feature se carga de forma diferida (lazy loading).
 * En HU-01 se agregará la ruta 'login' y un guard para proteger las demás.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'inicio' },
  {
    path: 'inicio',
    loadComponent: () =>
      import('./features/inicio/inicio.component').then((m) => m.InicioComponent),
  },
  { path: '**', redirectTo: 'inicio' },
];
