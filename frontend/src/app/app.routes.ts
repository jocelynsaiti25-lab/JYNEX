import { Routes } from '@angular/router';

import { cambiosSinGuardarGuard } from './core/guards/cambios-sin-guardar.guard';
import { rolGuard } from './core/guards/rol.guard';
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
    // HU-02: pantalla de paso para cerrar sesión. Va FUERA del LayoutComponent y sin sesionGuard:
    // si la sesión ya expiró (escenario 10) también debe poder cerrarse sin error.
    path: 'cerrar-sesion',
    title: 'Cerrando sesión · Etbot Farma',
    loadComponent: () =>
      import('./features/cierre-sesion/cerrar-sesion.component').then((m) => m.CerrarSesionComponent),
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
        // HU-03: solo el Administrador (criterio 1); avisa si sale con datos sin guardar (escenario 16)
        path: 'usuarios/crear',
        title: 'Crear usuario · Etbot Farma',
        canActivate: [rolGuard(['Administrador'])],
        canDeactivate: [cambiosSinGuardarGuard],
        loadComponent: () =>
          import('./features/creacion-usuarios/crear-usuario.component').then((m) => m.CrearUsuarioComponent),
      },
      {
        // HU-04: solo el Administrador; un Empleado que escribe la dirección regresa al inicio (escenario 2)
        path: 'usuarios',
        title: 'Usuarios · Etbot Farma',
        canActivate: [rolGuard(['Administrador'])],
        loadComponent: () =>
          import('./features/consulta-usuarios/consulta-usuarios.component').then((m) => m.ConsultaUsuariosComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'inicio' },
];
