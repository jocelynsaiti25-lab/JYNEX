import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';

import { SesionService } from '../services/sesion.service';
import { opcionesPara } from './menu';

/**
 * Estructura de todas las pantallas con sesión: barra lateral + contenido.
 *   Escritorio (>= 1024px): barra lateral fija a la izquierda.
 *   Celular/tableta: barra superior con botón "Menú" que abre la barra lateral.
 * Cada pantalla protegida se carga dentro de <router-outlet>.
 */
@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss',
  host: { '(document:keydown.escape)': 'cerrarMenu()' },
})
export class LayoutComponent {
  readonly sesion = inject(SesionService);

  /** Solo los módulos ya programados (con ruta) que el rol puede ver. */
  readonly opciones = computed(() =>
    opcionesPara(this.sesion.usuario()?.rol).filter((opcion) => opcion.ruta),
  );

  readonly iniciales = computed(() => {
    const nombre = this.sesion.usuario()?.nombreCompleto ?? '';
    return nombre
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((parte) => parte[0].toUpperCase())
      .join('');
  });

  /** Menú abierto en celular/tableta. */
  readonly menuAbierto = signal(false);

  /** HU-02 escenario 12: true mientras se está cerrando la sesión (desactiva el botón). */
  readonly cerrando = signal(false);

  private readonly router = inject(Router);

  constructor() {
    // Al cambiar de pantalla en celular, el menú se cierra solo
    this.router
      .events.pipe(
        filter((evento) => evento instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.cerrarMenu());
  }

  alternarMenu(): void {
    this.menuAbierto.update((abierto) => !abierto);
  }

  cerrarMenu(): void {
    this.menuAbierto.set(false);
  }

  /**
   * HU-02: cerrar sesión.
   * Navega a /cerrar-sesion. Si la pantalla actual tiene datos sin guardar, su canDeactivate
   * pregunta primero (escenarios 7 y 8). Si la persona cancela, la navegación devuelve false:
   * la sesión sigue activa, se queda en la misma pantalla y el botón se vuelve a activar.
   */
  cerrarSesion(): void {
    if (this.cerrando()) {
      return;   // escenario 12: ignorar clics repetidos
    }
    this.cerrando.set(true);

    this.router.navigateByUrl('/cerrar-sesion').then((salio) => {
      if (!salio) {
        this.cerrando.set(false);   // escenario 7: eligió "Cancelar"
      }
    });
  }
}
