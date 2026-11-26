import { Component, OnInit, inject, signal } from '@angular/core';

import { MedicamentoStock } from '../../../core/models/stock-minimo.model';
import { StockMinimoService } from '../../../core/services/stock-minimo.service';
import { AlertaComponent } from '../../../shared/components/alerta/alerta.component';
import { ConfirmacionComponent } from '../../../shared/components/confirmacion/confirmacion.component';

interface EdicionPendiente {
  medicamento: MedicamentoStock;
  nuevoValor: number;
}

/**
 * HU-11: control de stock mínimo del catálogo (Administrador y Empleado por igual).
 *   Escenario 1: define o edita el mínimo de cada medicamento (entero ≥ 0).
 *   Escenarios 2, 3, 4: existencias vigentes y alertas de stock bajo / agotado (del servidor).
 *   Escenario 5: tras guardar, se vuelve a consultar la lista con las alertas recalculadas.
 *   Escenario 6: confirmación previa y mensaje de resultado (<app-confirmacion>, <app-alerta>).
 */
@Component({
  selector: 'app-stock-minimo',
  imports: [AlertaComponent, ConfirmacionComponent],
  templateUrl: './stock-minimo.component.html',
  styleUrl: './stock-minimo.component.scss',
})
export class StockMinimoComponent implements OnInit {
  private readonly stockMinimoService = inject(StockMinimoService);

  readonly medicamentos = signal<MedicamentoStock[]>([]);
  readonly cargando = signal(false);
  readonly errorCarga = signal<string | null>(null);

  /** Valores que el usuario está editando, antes de confirmarlos (por idMedicamento). */
  readonly valoresEditados = signal<Record<number, number>>({});

  readonly pendiente = signal<EdicionPendiente | null>(null);
  readonly guardando = signal(false);
  readonly mensajeExito = signal<string | null>(null);
  readonly errorGuardado = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.errorCarga.set(null);

    this.stockMinimoService.obtenerParaControl().subscribe({
      next: (datos) => {
        this.medicamentos.set(datos);
        this.cargando.set(false);
      },
      error: () => {
        this.cargando.set(false);
        this.errorCarga.set('No se pudo cargar la lista de medicamentos.');
      },
    });
  }

  valorEditado(medicamento: MedicamentoStock): number {
    return this.valoresEditados()[medicamento.idMedicamento] ?? medicamento.stockMinimo;
  }

  actualizarValor(idMedicamento: number, valor: string): void {
    const numero = Math.trunc(Number(valor));
    this.valoresEditados.update((actual) => ({
      ...actual,
      [idMedicamento]: Number.isNaN(numero) ? 0 : Math.max(0, numero),
    }));
  }

  hayCambio(medicamento: MedicamentoStock): boolean {
    return this.valorEditado(medicamento) !== medicamento.stockMinimo;
  }

  revertir(medicamento: MedicamentoStock): void {
    this.valoresEditados.update((actual) => {
      const { [medicamento.idMedicamento]: _omitido, ...resto } = actual;
      return resto;
    });
  }

  /** Escenario 6: pide confirmación antes de guardar. */
  pedirConfirmacion(medicamento: MedicamentoStock): void {
    if (!this.hayCambio(medicamento)) return;

    this.mensajeExito.set(null);
    this.errorGuardado.set(null);
    this.pendiente.set({ medicamento, nuevoValor: this.valorEditado(medicamento) });
  }

  cancelarConfirmacion(): void {
    if (this.guardando()) return;
    this.pendiente.set(null);
  }

  /** Escenario 6: confirma y envía la petición una sola vez. */
  confirmarCambio(): void {
    const edicion = this.pendiente();
    if (!edicion || this.guardando()) return;

    this.guardando.set(true);
    this.stockMinimoService.actualizar(edicion.medicamento.idMedicamento, { nuevoStockMinimo: edicion.nuevoValor }).subscribe({
      next: (resultado) => {
        this.guardando.set(false);
        this.pendiente.set(null);
        this.revertir(edicion.medicamento);
        this.mensajeExito.set(resultado.mensaje);
        this.cargar(); // escenario 5: trae las alertas ya recalculadas por el servidor
      },
      error: () => {
        this.guardando.set(false);
        this.pendiente.set(null);
        this.errorGuardado.set('No se pudo actualizar el stock mínimo. Revisa tu conexión e intenta de nuevo.');
      },
    });
  }
}
