import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

let contador = 0;

/**
 * Ventana de confirmación (modal) para las historias que piden
 * "confirmación antes de guardar": HU-02, 03, 05, 06, 07, 09, 11, 13, 14 y 16.
 *
 * Usa <dialog> nativo del navegador: atrapa el foco del teclado,
 * se cierra con Esc y lo anuncia el lector de pantalla.
 *
 *   <app-confirmacion
 *     [abierta]="confirmando()"
 *     titulo="¿Registrar la salida?"
 *     textoConfirmar="Registrar salida"
 *     (confirmar)="guardar()"
 *     (cancelar)="confirmando.set(false)">
 *     <!-- resumen: medicamento, cantidad, lotes... -->
 *   </app-confirmacion>
 *
 * tipo="peligro" pinta el botón en rojo (solo acciones irreversibles, p. ej. HU-14).
 */
@Component({
  selector: 'app-confirmacion',
  template: `
    <dialog
      #dialogo
      class="confirmacion"
      [attr.aria-labelledby]="idTitulo"
      (cancel)="$event.preventDefault(); cancelar.emit()"
    >
      <div class="confirmacion__icono" [class.confirmacion__icono--peligro]="tipo() === 'peligro'" aria-hidden="true">
        <span class="icono">{{ icono() }}</span>
      </div>
      <h2 [id]="idTitulo">{{ titulo() }}</h2>
      <div class="confirmacion__cuerpo">
        <ng-content />
      </div>
      <div class="acciones">
        <button type="button" class="boton boton--secundario" (click)="cancelar.emit()" [disabled]="procesando()">
          {{ textoCancelar() }}
        </button>
        <button
          type="button"
          class="boton"
          [class.boton--primario]="tipo() === 'primario'"
          [class.boton--peligro]="tipo() === 'peligro'"
          [disabled]="procesando()"
          (click)="confirmar.emit()"
        >
          @if (procesando()) {
            <span class="cargando" aria-hidden="true"></span>
          }
          {{ textoConfirmar() }}
        </button>
      </div>
    </dialog>
  `,
  styles: `
    .confirmacion {
      width: min(32rem, calc(100vw - 2rem));
      padding: var(--espacio-6);
      color: var(--color-texto);
      background: var(--color-superficie);
      border: 1px solid var(--color-borde);
      border-radius: var(--radio-xl);
      box-shadow: var(--sombra-modal);
    }
    .confirmacion::backdrop {
      background: var(--velo-modal);
      backdrop-filter: blur(4px);
    }
    .confirmacion__icono {
      display: grid;
      place-items: center;
      width: 3.5rem;
      height: 3.5rem;
      margin-bottom: var(--espacio-4);
      color: var(--color-texto-invertido);
      background: var(--color-primario);
      border-radius: var(--radio-lg);
      box-shadow: var(--sombra-boton);
    }
    .confirmacion__icono--peligro { background: var(--color-error); }
    .confirmacion__icono .icono { font-size: 1.75rem; }
    .confirmacion__cuerpo {
      margin: var(--espacio-3) 0 var(--espacio-6);
      color: var(--color-texto-secundario);
    }
  `,
})
export class ConfirmacionComponent {
  /** id único para enlazar el título con el diálogo (accesibilidad). */
  readonly idTitulo = `confirmacion-titulo-${++contador}`;

  readonly abierta = input(false);
  readonly titulo = input.required<string>();
  readonly textoConfirmar = input('Confirmar');
  readonly textoCancelar = input('Cancelar');
  readonly tipo = input<'primario' | 'peligro'>('primario');
  readonly icono = input('help');
  /** true mientras se espera la respuesta de la API (deshabilita los botones). */
  readonly procesando = input(false);

  readonly confirmar = output<void>();
  readonly cancelar = output<void>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    effect(() => {
      const dialogo = this.dialogo().nativeElement;
      if (this.abierta() && !dialogo.open) {
        dialogo.showModal();
      } else if (!this.abierta() && dialogo.open) {
        dialogo.close();
      }
    });
  }
}
