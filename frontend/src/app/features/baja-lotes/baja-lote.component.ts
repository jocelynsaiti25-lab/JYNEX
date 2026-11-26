import { HttpErrorResponse } from '@angular/common/http';
import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';

import { ResultadoBaja } from '../../core/models/baja-lote.model';
import { ItemInventario } from '../../core/models/inventario.model';
import { BajaLotesService } from '../../core/services/baja-lotes.service';
import { ConfirmacionComponent } from '../../shared/components/confirmacion/confirmacion.component';

export const LONGITUD_MAXIMA_MOTIVO = 200;

export function validarMotivo(texto: string): string | null {
  const motivo = texto.trim();
  if (!motivo) return 'El motivo es obligatorio';
  if (motivo.length > LONGITUD_MAXIMA_MOTIVO) return 'El motivo no puede tener más de 200 caracteres';
  return null;
}

export function puedeDarseDeBaja(item: ItemInventario): boolean {
  return item.idLote !== null && item.semaforoCaducidad === 'Rojo' && item.existencias > 0;
}

interface ErrorApi {
  detail?: string;
  errores?: string[];
}

type Paso = 'motivo' | 'confirmar' | null;

@Component({
  selector: 'app-baja-lote',
  standalone: true,
  imports: [ConfirmacionComponent],
  templateUrl: './baja-lote.component.html',
  styleUrl: './baja-lote.component.scss',
})
export class BajaLoteComponent {
  private readonly bajaLotesService = inject(BajaLotesService);

  readonly lote = input<ItemInventario | null>(null);
  readonly terminada = output<ResultadoBaja>();
  readonly cancelada = output<void>();

  readonly paso = signal<Paso>(null);
  readonly motivo = signal('');
  readonly errorMotivo = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly longitudMaxima = LONGITUD_MAXIMA_MOTIVO;

  constructor() {
    effect(() => {
      const lote = this.lote();
      untracked(() => {
        this.motivo.set('');
        this.errorMotivo.set(null);
        this.enviando.set(false);
        this.paso.set(lote ? 'motivo' : null);
      });
    });
  }

  actualizarMotivo(valor: string): void {
    this.motivo.set(valor);
    if (this.errorMotivo()) {
      this.errorMotivo.set(validarMotivo(valor));
    }
  }

  continuar(): void {
    const error = validarMotivo(this.motivo());
    this.errorMotivo.set(error);
    if (error) return;
    this.paso.set('confirmar');
  }

  confirmar(): void {
    const lote = this.lote();
    if (!lote || lote.idLote === null || this.enviando()) return;

    this.enviando.set(true);
    this.bajaLotesService
      .darDeBaja(lote.idLote, { cantidadConsultada: lote.existencias, motivo: this.motivo().trim() })
      .subscribe({
        next: (resultado) => {
          this.paso.set(null);
          this.enviando.set(false);
          this.terminada.emit({ tipo: 'exito', mensaje: resultado.mensaje || 'Lote dado de baja' });
        },
        error: (error: HttpErrorResponse) => {
          this.paso.set(null);
          this.enviando.set(false);
          this.terminada.emit({ tipo: 'error', mensaje: mensajeDeError(error) });
        },
      });
  }

  cancelar(): void {
    if (this.enviando()) return;
    this.paso.set(null);
    this.cancelada.emit();
  }
}

function mensajeDeError(error: HttpErrorResponse): string {
  const cuerpo = (error.error ?? {}) as ErrorApi;
  if (error.status === 422) return 'El lote ya no es elegible';
  if (error.status === 404) return cuerpo.detail ?? 'El lote no se encontró.';
  if (error.status === 400 && cuerpo.errores?.length) return cuerpo.errores.join(' ');
  return 'No se pudo dar de baja el lote';
}
