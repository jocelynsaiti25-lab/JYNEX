import { Component, computed, input, output } from '@angular/core';

export type TipoAlerta = 'exito' | 'aviso' | 'error' | 'info';

const ICONOS: Record<TipoAlerta, string> = {
  exito: 'check_circle',
  aviso: 'warning',
  error: 'error',
  info: 'info',
};

/**
 * Mensaje de éxito, aviso, error o información.
 * Los errores se anuncian de inmediato al lector de pantalla (role="alert");
 * los demás, de forma educada (role="status").
 *
 *   <app-alerta tipo="error" titulo="No se pudo guardar">Inténtalo de nuevo.</app-alerta>
 *   <app-alerta tipo="exito" [cerrable]="true" (cerrar)="ocultar()">Usuario creado.</app-alerta>
 */
@Component({
  selector: 'app-alerta',
  template: `
    <div [class]="'alerta alerta--' + tipo()" [attr.role]="tipo() === 'error' ? 'alert' : 'status'">
      <span class="icono" aria-hidden="true">{{ icono() }}</span>
      <div class="alerta__texto">
        @if (titulo()) {
          <strong class="alerta__titulo">{{ titulo() }}</strong>
        }
        <ng-content />
      </div>
      @if (cerrable()) {
        <button type="button" class="campo__boton alerta__cerrar" (click)="cerrar.emit()">
          <span class="icono" aria-hidden="true">close</span>
          <span class="solo-lector">Cerrar mensaje</span>
        </button>
      }
    </div>
  `,
  styles: `
    .alerta__texto { flex: 1; padding-top: 0.125rem; }
    .alerta__cerrar { margin: calc(var(--espacio-2) * -1); color: inherit; }
  `,
})
export class AlertaComponent {
  readonly tipo = input<TipoAlerta>('info');
  readonly titulo = input<string>();
  readonly cerrable = input(false);
  readonly cerrar = output<void>();

  readonly icono = computed(() => ICONOS[this.tipo()]);
}
