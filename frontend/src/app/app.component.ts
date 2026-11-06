import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Componente raíz. Solo muestra la ruta actual:
 *   - /iniciar-sesion se ve a pantalla completa.
 *   - Las pantallas con sesión se ven dentro de LayoutComponent (barra lateral).
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class AppComponent {}
