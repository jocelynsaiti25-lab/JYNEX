import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { opcionesPara } from '../../core/layout/menu';
import { EstadoSistema } from '../../core/models/estado-sistema.model';
import { SesionService } from '../../core/services/sesion.service';
import { SistemaService } from '../../core/services/sistema.service';
import { SemaforoComponent } from '../../shared/components/semaforo/semaforo.component';

/**
 * Módulo inicial (HU-01 criterio 3). Protegido: solo se ve con sesión activa.
 * Muestra la bienvenida, los módulos que puede usar su rol (los que aún no
 * se programan aparecen como "Próximamente") y la guía del semáforo (HU-10).
 * Diseño: Stitch "Menú principal y bienvenida", sin datos inventados.
 */
@Component({
  selector: 'app-inicio',
  imports: [DatePipe, RouterLink, SemaforoComponent],
  templateUrl: './inicio.component.html',
  styleUrl: './inicio.component.scss',
})
export class InicioComponent implements OnInit {
  private readonly sistemaService = inject(SistemaService);
  readonly sesion = inject(SesionService);

  readonly hoy = new Date();
  readonly estado = signal<EstadoSistema | null>(null);
  readonly errorApi = signal(false);

  /** Módulos del rol, sin contar "Inicio". */
  readonly modulos = computed(() =>
    opcionesPara(this.sesion.usuario()?.rol).filter((opcion) => opcion.ruta !== '/inicio'),
  );

  ngOnInit(): void {
    this.sistemaService.obtenerEstado().subscribe({
      next: (estado) => this.estado.set(estado),
      error: () => this.errorApi.set(true),
    });
  }
}
