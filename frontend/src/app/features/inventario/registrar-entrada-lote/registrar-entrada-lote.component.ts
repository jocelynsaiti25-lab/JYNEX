import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';


import { ConCambiosSinGuardar } from '../../../core/guards/cambios-sin-guardar.guard';
import {
  MedicamentoActivo,
  RegistrarEntradaLoteResponse,
} from '../../../core/models/entrada-lote.model';
import { EntradaLoteService } from '../../../core/services/entrada-lote.service';

@Component({
  selector: 'app-registrar-entrada-lote',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './registrar-entrada-lote.component.html',
  styleUrl: './registrar-entrada-lote.component.scss',
})
export class RegistrarEntradaLoteComponent implements OnInit, ConCambiosSinGuardar {
  private readonly fb = inject(FormBuilder);
  private readonly entradaLoteService = inject(EntradaLoteService);

  // Estado de búsqueda y selección (Escenarios 1 y 2)
  readonly terminoBusqueda = signal('');
  readonly cargandoCatalogo = signal(false);
  readonly medicamentosDisponibles = signal<MedicamentoActivo[]>([]);
  readonly medicamentoSeleccionado = signal<MedicamentoActivo | null>(null);

  // Estado de confirmación, envío y mensajes (Escenarios 8, 9, 10, 11, 13 y 14)
  readonly mostrandoResumen = signal(false);
  readonly guardando = signal(false);
  readonly errorServidor = signal<string | null>(null);
  readonly erroresValidacion = signal<string[]>([]);
  readonly resultadoExitoso = signal<RegistrarEntradaLoteResponse | null>(null);

  readonly fechaMinimaManana = computed(() => {
    const manana = new Date();
    manana.setDate(manana.getDate() + 1);
    return manana.toISOString().split('T')[0];
  });

  readonly formulario = this.fb.nonNullable.group({
    numeroLote: [
      '',
      [
        Validators.required,
        Validators.maxLength(50),
        Validators.pattern(/^[a-zA-Z0-9-]+$/),
      ],
    ],
    fechaCaducidad: ['', [Validators.required]],
    cantidad: ['', [Validators.required, Validators.pattern(/^[1-9]\d*$/)]],
  });

  // Cálculo preventivo del semáforo en cliente (Escenario 12)
  readonly semaforoPreventivo = signal<'Verde' | 'Amarillo' | 'Rojo' | 'Caducado' | null>(null);

  ngOnInit(): void {
    this.buscarMedicamentos();
    this.formulario.controls.fechaCaducidad.valueChanges.subscribe((fecha) => {
      this.evaluarSemaforoPreventivo(fecha);
    });
  }

  /** HU-07 escenario 14: avisa si intenta salir con cambios sin guardar. */
  puedeSalir(): boolean {
    if (this.guardando()) return false;
    if (!this.formulario.dirty) return true;
    return confirm('Tienes datos sin guardar en el formulario de entrada de lote. ¿Seguro que deseas salir?');
  }

  buscarMedicamentos(): void {
    this.cargandoCatalogo.set(true);
    this.entradaLoteService.buscarMedicamentosActivos(this.terminoBusqueda()).subscribe({
      next: (lista) => {
        this.medicamentosDisponibles.set(lista);
        this.cargandoCatalogo.set(false);
      },
      error: () => {
        this.cargandoCatalogo.set(false);
        this.errorServidor.set('No se pudo cargar el catálogo de medicamentos activos.');
      },
    });
  }

  seleccionarMedicamento(med: MedicamentoActivo): void {
    this.medicamentoSeleccionado.set(med);
    this.resultadoExitoso.set(null);
    this.errorServidor.set(null);
    this.erroresValidacion.set([]);
  }

  limpiarSeleccion(): void {
    this.medicamentoSeleccionado.set(null);
    this.mostrandoResumen.set(false);
    this.formulario.reset({ numeroLote: '', fechaCaducidad: '', cantidad: '' });
    this.semaforoPreventivo.set(null);
  }

  /** HU-07 escenarios 3, 4, 5 y 8: valida formulario y abre modal de resumen previo. */
  solicitarConfirmacion(): void {
    this.errorServidor.set(null);
    this.erroresValidacion.set([]);

    if (!this.medicamentoSeleccionado()) {
      this.errorServidor.set('Debes seleccionar un medicamento activo del catálogo antes de continuar.');
      return;
    }

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    if (this.semaforoPreventivo() === 'Caducado') {
      this.errorServidor.set('La fecha de caducidad debe ser posterior a la fecha actual.');
      return;
    }

    this.mostrandoResumen.set(true);
  }

  /** HU-07 escenario 9: cierra el resumen previo sin guardar y conserva los datos capturados. */
  cancelarConfirmacion(): void {
    this.mostrandoResumen.set(false);
  }

  /** HU-07 escenarios 10, 11, 13 y 14: confirma el registro atómico en el servidor. */
  confirmarRegistro(): void {
    const med = this.medicamentoSeleccionado();
    if (!med || this.guardando()) return;

    this.guardando.set(true);
    this.errorServidor.set(null);
    this.erroresValidacion.set([]);

    const valores = this.formulario.getRawValue();

    this.entradaLoteService
      .registrar({
        idMedicamento: med.idMedicamento,
        numeroLote: valores.numeroLote.trim(),
        fechaCaducidad: valores.fechaCaducidad,
        cantidad: String(valores.cantidad).trim(),
      })
      .subscribe({
        next: (respuesta) => {
          this.guardando.set(false);
          this.mostrandoResumen.set(false);
          this.resultadoExitoso.set(respuesta);
          this.formulario.reset({ numeroLote: '', fechaCaducidad: '', cantidad: '' });
          this.semaforoPreventivo.set(null);
          // Actualiza la lista de medicamentos para reflejar nuevas existencias y estado agotado/stock bajo
          this.buscarMedicamentos();
          this.medicamentoSeleccionado.set({
            ...med,
            existenciasTotales: respuesta.existenciasTotalesActualizadas,
            semaforoGeneral: respuesta.semaforoGeneralActualizado,
            estaAgotado: respuesta.estaAgotado,
            enStockBajo: respuesta.enStockBajo,
          });
        },
        error: (err) => {
          this.guardando.set(false);
          this.mostrandoResumen.set(false);

          if (err.status === 400 && Array.isArray(err.error?.errores)) {
            this.erroresValidacion.set(err.error.errores);
          } else if (err.status === 422 && err.error?.detail) {
            this.errorServidor.set(err.error.detail);
          } else {
            this.errorServidor.set('Ocurrió un error al registrar la entrada de lote. No se guardaron cambios.');
          }
        },
      });
  }

  private evaluarSemaforoPreventivo(fechaStr: string): void {
    if (!fechaStr) {
      this.semaforoPreventivo.set(null);
      return;
    }

    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);

    const [anio, mes, dia] = fechaStr.split('-').map(Number);
    const caducidad = new Date(anio, mes - 1, dia);

    if (caducidad <= hoy) {
      this.semaforoPreventivo.set('Caducado');
      return;
    }

    const masTresMeses = new Date(hoy);
    masTresMeses.setMonth(masTresMeses.getMonth() + 3);

    const masDoceMeses = new Date(hoy);
    masDoceMeses.setMonth(masDoceMeses.getMonth() + 12);

    if (caducidad <= masTresMeses) {
      this.semaforoPreventivo.set('Rojo');
    } else if (caducidad <= masDoceMeses) {
      this.semaforoPreventivo.set('Amarillo');
    } else {
      this.semaforoPreventivo.set('Verde');
    }
  }
}