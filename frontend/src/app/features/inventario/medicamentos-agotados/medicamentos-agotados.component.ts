import { Component, OnInit, inject, signal } from '@angular/core';

import { MedicamentoAgotado } from '../../../core/models/medicamento-agotado.model';
import { MedicamentosAgotadosService } from '../../../core/services/medicamentos-agotados.service';
import { AlertaComponent } from '../../../shared/components/alerta/alerta.component';

/**
 * HU-12: consulta de medicamentos agotados (Administrador y Empleado por igual).
 *   Escenarios 1 y 2: lista independiente del semáforo de caducidad; agotado = cero existencias
 *     vigentes, aunque existan lotes caducados (Medicamento.CalcularExistenciasVigentes).
 *   Escenario 3: nombre, presentación, concentración y etiqueta "SIN EXISTENCIAS".
 *   Escenarios 4 y 6: la lista se recalcula en cada consulta (sin estado guardado), así que
 *     cualquier entrada, salida o baja ya queda reflejada sin trabajo extra.
 */
@Component({
  selector: 'app-medicamentos-agotados',
  imports: [AlertaComponent],
  templateUrl: './medicamentos-agotados.component.html',
})
export class MedicamentosAgotadosComponent implements OnInit {
  private readonly medicamentosAgotadosService = inject(MedicamentosAgotadosService);

  readonly medicamentos = signal<MedicamentoAgotado[]>([]);
  readonly cargando = signal(false);
  readonly errorCarga = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.cargando.set(true);
    this.errorCarga.set(null);

    this.medicamentosAgotadosService.obtenerAgotados().subscribe({
      next: (datos) => {
        this.medicamentos.set(datos);
        this.cargando.set(false);
      },
      error: () => {
        this.cargando.set(false);
        this.errorCarga.set('No se pudo cargar la lista de medicamentos agotados.');
      },
    });
  }
}
