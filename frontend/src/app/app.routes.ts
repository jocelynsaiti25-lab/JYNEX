import { Routes } from '@angular/router';

import { sesionGuard, invitadoGuard } from './core/guards/sesion.guard';

/**
 * Rutas de la aplicación. Cada pantalla se carga solo cuando se necesita (lazy loading).
 *   iniciar-sesion → pública (si ya hay sesión, manda al inicio)
 *   todo lo demás  → protegido (HU-01 criterio 5) y dentro del LayoutComponent
 *
 * PARA AGREGAR UNA PANTALLA: añádela en `children` y pon su ruta en core/layout/menu.ts.
 */
export const routes: Routes = [
  {
    path: 'iniciar-sesion',
    canActivate: [invitadoGuard],
    title: 'Iniciar sesión · Etbot Farma',
    loadComponent: () =>
      import('./features/inicio-sesion/iniciar-sesion.component').then((m) => m.IniciarSesionComponent),
  },
  {
    path: '',
    // Se revisa la sesión en CADA cambio de pantalla protegida
    canActivateChild: [sesionGuard],
    loadComponent: () => import('./core/layout/layout.component').then((m) => m.LayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      {
        path: 'inicio',
        title: 'Inicio · Etbot Farma',
        loadComponent: () => import('./features/inicio/inicio.component').then((m) => m.InicioComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'inicio' },
];
