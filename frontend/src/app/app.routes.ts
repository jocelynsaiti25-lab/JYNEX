import { Routes } from '@angular/router';

import { cambiosSinGuardarGuard } from './core/guards/cambios-sin-guardar.guard';
import { sesionGuard, invitadoGuard } from './core/guards/sesion.guard';

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
      {
        path: 'medicamentos/registrar',
        canDeactivate: [cambiosSinGuardarGuard],
        title: 'Registrar medicamento · Etbot Farma',
        loadComponent: () =>
          import('./features/medicamentos/registrar-medicamento/registrar-medicamento.component').then(
            (m) => m.RegistrarMedicamentoComponent
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'inicio' },
];