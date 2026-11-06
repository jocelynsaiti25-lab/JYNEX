import { Component, computed, input } from '@angular/core';

/**
 * Estados que devuelve la API (la regla de meses se calcula en el backend, HU-10).
 *   verde    → caduca en más de 12 meses
 *   amarillo → caduca en más de 3 y hasta 12 meses
 *   rojo     → caduca en 3 meses o menos
 *   caducado → ya vencido (también es rojo; no se puede usar en salidas)
 *   agotado  → sin existencias vigentes (HU-12)
 */
export type EstadoSemaforo = 'verde' | 'amarillo' | 'rojo' | 'caducado' | 'agotado';

const ESTADOS: Record<EstadoSemaforo, { texto: string; icono: string }> = {
  verde: { texto: 'Verde', icono: 'check_circle' },
  amarillo: { texto: 'Amarillo', icono: 'warning' },
  rojo: { texto: 'Rojo', icono: 'error' },
  caducado: { texto: 'CADUCADO', icono: 'block' },
  agotado: { texto: 'SIN EXISTENCIAS', icono: 'remove_shopping_cart' },
};

/**
 * Etiqueta del semáforo de caducidad.
 * Accesible: nunca depende solo del color, siempre lleva ícono + texto.
 *
 *   <app-semaforo estado="amarillo" detalle="vence en 5 meses" />
 */
@Component({
  selector: 'app-semaforo',
  template: `
    <span [class]="'semaforo semaforo--' + estado()">
      <span class="icono" aria-hidden="true">{{ datos().icono }}</span>
      {{ datos().texto }}
      @if (detalle()) {
        <span class="semaforo__detalle">· {{ detalle() }}</span>
      }
    </span>
  `,
  styles: `
    .semaforo {
      display: inline-flex;
      align-items: center;
      gap: var(--espacio-1);
      padding: 0.125rem var(--espacio-3);
      font-size: var(--texto-xs);
      font-weight: var(--peso-semi);
      letter-spacing: 0.02em;
      white-space: nowrap;
      border: 1px solid;
      border-radius: var(--radio-pildora);
    }
    .icono { font-size: 1rem; }
    .semaforo__detalle { font-weight: var(--peso-medio); }
    .semaforo--verde {
      color: var(--semaforo-verde);
      background: var(--semaforo-verde-fondo);
      border-color: var(--semaforo-verde-borde);
    }
    .semaforo--amarillo {
      color: var(--semaforo-amarillo);
      background: var(--semaforo-amarillo-fondo);
      border-color: var(--semaforo-amarillo-borde);
    }
    .semaforo--rojo,
    .semaforo--caducado {
      color: var(--semaforo-rojo);
      background: var(--semaforo-rojo-fondo);
      border-color: var(--semaforo-rojo-borde);
    }
    .semaforo--agotado {
      color: var(--semaforo-agotado);
      background: var(--semaforo-agotado-fondo);
      border-color: var(--semaforo-agotado-borde);
    }
  `,
})
export class SemaforoComponent {
  readonly estado = input.required<EstadoSemaforo>();
  readonly detalle = input<string>();

  readonly datos = computed(() => ESTADOS[this.estado()]);
}
