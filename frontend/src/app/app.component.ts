import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Componente raíz: estructura general (encabezado + contenido de cada ruta). */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `
    <header class="encabezado">
      <strong>ETBOTFARMA WEB</strong>
    </header>
    <main class="contenido">
      <router-outlet />
    </main>
  `,
  styles: `
    .encabezado {
      padding: 1rem 1.5rem;
      background: #0f6e56;
      color: #fff;
    }
    .contenido {
      padding: 1.5rem;
    }
  `,
})
export class AppComponent {}
