import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';

import { EstadoSistema } from '../../core/models/estado-sistema.model';
import { SistemaService } from '../../core/services/sistema.service';

/**
 * Pantalla de inicio temporal: muestra si Angular se conecta con la API.
 * Se reemplazará por el módulo inicial según permisos (HU-01).
 */
@Component({
  selector: 'app-inicio',
  imports: [DatePipe],
  template: `
    <h1>Solución base funcionando</h1>

    @if (cargando()) {
      <p>Conectando con la API...</p>
    } @else {
      @if (estado(); as e) {
        <p class="ok">API conectada: {{ e.aplicacion }} v{{ e.version }}</p>
        <p>Hora del servidor: {{ e.fechaServidorUtc | date: 'medium' }}</p>
      } @else {
        <p class="error">{{ error() }}</p>
      }
    }
  `,
  styles: `
    .ok { color: #0f6e56; }
    .error { color: #a32d2d; }
  `,
})
export class InicioComponent implements OnInit {
  private readonly sistemaService = inject(SistemaService);

  readonly estado = signal<EstadoSistema | null>(null);
  readonly error = signal<string | null>(null);
  readonly cargando = signal(true);

  ngOnInit(): void {
    this.sistemaService.obtenerEstado().subscribe({
      next: (estado) => {
        this.estado.set(estado);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo conectar con la API. ¿Está ejecutándose en el puerto 5080?');
        this.cargando.set(false);
      },
    });
  }
}
