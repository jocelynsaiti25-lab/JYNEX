import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';

import { SesionService } from '../../core/services/sesion.service';

/**
 * HU-02: cierre seguro de sesión.
 *
 * Es una "pantalla de paso": el botón "Cerrar sesión" del menú navega a /cerrar-sesion.
 * Gracias a eso, si la pantalla actual tiene datos sin guardar, su canDeactivate pregunta
 * ANTES de cerrar (escenarios 7 y 8): si el usuario cancela, nunca se llega aquí y la sesión
 * sigue activa con los datos intactos. Sin cambios pendientes, se llega directo (escenario 1).
 *
 * Al llegar:
 *   1. Pide al servidor cerrar la sesión UNA sola vez (escenario 12).
 *   2. El servicio limpia los datos de sesión del navegador, con o sin respuesta (escenarios 10 y 11).
 *   3. Manda al inicio de sesión REEMPLAZANDO esta página en el historial (replaceUrl),
 *      así el botón Atrás no regresa aquí (escenario 3).
 */
@Component({
  selector: 'app-cerrar-sesion',
  template: `
    <main class="cerrando">
      <span class="cargando" aria-hidden="true"></span>
      <p role="status">Cerrando sesión…</p>
    </main>
  `,
  styles: `
    .cerrando {
      display: grid;
      place-content: center;
      justify-items: center;
      gap: var(--espacio-3);
      min-height: 100dvh;
      color: var(--color-texto-secundario);
    }
  `,
})
export class CerrarSesionComponent implements OnInit {
  private readonly sesion = inject(SesionService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.sesion.cerrarSesion().subscribe({
      // Escenario 1: "Sesión cerrada correctamente"
      next: () => this.irAlInicioDeSesion('1'),
      // Escenario 11: se sale igual, pero avisando que hubo un problema de conexión
      error: () => this.irAlInicioDeSesion('sin-conexion'),
    });
  }

  private irAlInicioDeSesion(cerrada: string): void {
    this.router.navigate(['/iniciar-sesion'], { queryParams: { cerrada }, replaceUrl: true });
  }
}
