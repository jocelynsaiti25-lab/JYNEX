import { HttpErrorResponse } from '@angular/common/http';
import { Component, HostListener, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';

import { ConCambiosSinGuardar } from '../../../core/guards/cambios-sin-guardar.guard';
import {
  RegistrarMedicamentoRequest,
  RegistrarMedicamentoResponse,
} from '../../../core/models/medicamento.model';
import { MedicamentosService } from '../../../core/services/medicamentos.service';
import { AlertaComponent } from '../../../shared/components/alerta/alerta.component';

/**
 * HU-06: Registro de medicamentos y su lote inicial.
 * Cubre los 20 escenarios de aceptación (validaciones, resumen antes de guardar,
 * aviso de semáforo rojo <= 3 meses, bloqueo de doble clic y aviso al salir sin guardar).
 */
@Component({
  selector: 'app-registrar-medicamento',
  imports: [ReactiveFormsModule, AlertaComponent],
  templateUrl: './registrar-medicamento.component.html',
  styleUrl: './registrar-medicamento.component.scss',
})
export class RegistrarMedicamentoComponent implements ConCambiosSinGuardar {
  private readonly fb = inject(FormBuilder);
  private readonly medicamentosService = inject(MedicamentosService);

  readonly guardando = signal(false);
  readonly mostrarConfirmacion = signal(false);
  readonly advertenciaCaducidadCorta = signal(false);
  readonly mensajeExito = signal<string | null>(null);
  readonly mensajeError = signal<string | null>(null);
  readonly ultimoRegistro = signal<RegistrarMedicamentoResponse | null>(null);

  readonly formulario = this.fb.nonNullable.group({
    nombreComercial: ['', [this.campoObligatorioValidator()]],
    nombreSustancia: ['', [this.campoObligatorioValidator()]],
    presentacion: ['', [this.campoObligatorioValidator()]],
    concentracion: ['', [this.campoObligatorioValidator()]],
    esControlado: [false],
    numeroLote: ['', [this.campoObligatorioValidator(), this.numeroLoteValidator()]],
    fechaCaducidad: ['', [this.campoObligatorioValidator(), this.fechaCaducidadValidator()]],
    cantidadInicial: ['', [this.campoObligatorioValidator(), this.cantidadEnteraValidator()]],
  });

  /**
   * Datos que se muestran en la confirmación y que se envían al servidor.
   * Es un signal que se actualiza al abrir la confirmación: un `computed` sobre el formulario
   * NO se recalcula (el formulario no es un signal) y mostraría siempre el primer registro.
   */
  readonly datosResumen = signal<RegistrarMedicamentoRequest>(this.construirDatos());

  /** HU-06 escenario 19: advierte si intenta cerrar o recargar la pestaña con datos sin guardar. */
  @HostListener('window:beforeunload', ['$event'])
  alCerrarPestana(event: BeforeUnloadEvent): void {
    if (this.formulario.dirty && !this.guardando()) {
      event.preventDefault();
    }
  }

  /** Usado por cambiosSinGuardarGuard al navegar dentro de la app o cerrar sesión. */
  puedeSalir(): boolean {
    if (!this.formulario.dirty || this.guardando()) {
      return true;
    }

    return window.confirm(
      'Tienes datos capturados sin guardar. Si sales ahora, se perderán. ¿Deseas salir de todos modos?'
    );
  }

  /** HU-06 escenarios 4, 8, 9, 10, 11, 12 y 13: valida y abre el resumen antes de guardar. */
  solicitarConfirmacion(): void {
    this.mensajeExito.set(null);
    this.mensajeError.set(null);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.enfocarPrimerCampoInvalido();
      return;
    }

    // HU-06 escenario 11: calcular si caduca en 3 meses o menos para mostrar aviso en el resumen
    const fecha = this.parsearFechaIso(this.formulario.controls.fechaCaducidad.value);
    this.advertenciaCaducidadCorta.set(fecha !== null && this.caducaEnTresMesesOMenos(fecha));
    this.datosResumen.set(this.construirDatos());
    this.mostrarConfirmacion.set(true);
  }

  /** HU-06 escenario 5: cierra la confirmación y conserva todos los datos en el formulario. */
  cancelarConfirmacion(): void {
    if (this.guardando()) return;
    this.mostrarConfirmacion.set(false);
  }

  /** HU-06 escenarios 1, 2, 3, 14, 16, 17 y 18: confirma el alta y envía la petición una sola vez. */
  confirmarRegistro(): void {
    // HU-06 escenario 18: evita envíos duplicados ante clics repetidos
    if (this.guardando()) return;

    this.guardando.set(true);
    this.mensajeError.set(null);

    const payload = this.datosResumen();

    this.medicamentosService.registrar(payload).subscribe({
      next: (respuesta) => {
        this.guardando.set(false);
        this.mostrarConfirmacion.set(false);
        this.ultimoRegistro.set(respuesta);
        // HU-06 escenario 1: muestra exactamente "Medicamento registrado"
        this.mensajeExito.set(respuesta.mensaje || 'Medicamento registrado');
        // Reinicia el formulario limpio para permitir un siguiente registro sin disparar el guard
        this.formulario.reset({
          nombreComercial: '',
          nombreSustancia: '',
          presentacion: '',
          concentracion: '',
          esControlado: false,
          numeroLote: '',
          fechaCaducidad: '',
          cantidadInicial: '',
        });
      },
      error: (error: HttpErrorResponse) => {
        this.guardando.set(false);
        this.mostrarConfirmacion.set(false);
        // HU-06 escenarios 14 y 17: conserva los datos capturados y muestra el mensaje adecuado
        this.mensajeError.set(this.obtenerMensajeError(error));
      },
    });
  }

  obtenerErrorCampo(
    nombreCampo:
      | 'nombreComercial'
      | 'nombreSustancia'
      | 'presentacion'
      | 'concentracion'
      | 'numeroLote'
      | 'fechaCaducidad'
      | 'cantidadInicial'
  ): string | null {
    const control = this.formulario.controls[nombreCampo];
    if (!control.touched || !control.errors) {
      return null;
    }
    const primerError = Object.values(control.errors)[0];
    return typeof primerError === 'string' ? primerError : 'Este campo es obligatorio';
  }

  // --- Validadores síncronos propios con mensajes exactos de la historia ---

  private campoObligatorioValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const valor = control.value === null || control.value === undefined ? '' : String(control.value).trim();
      return valor.length === 0 ? { obligatorio: 'Este campo es obligatorio' } : null;
    };
  }

  private numeroLoteValidator(): ValidatorFn {
    const regexLote = /^[a-zA-Z0-9-]+$/;
    return (control: AbstractControl): ValidationErrors | null => {
      const valor = String(control.value ?? '').trim();
      if (!valor) return null; // Lo cubre campoObligatorioValidator
      if (valor.length > 50 || !regexLote.test(valor)) {
        return { loteInvalido: 'El número de lote solo puede tener letras, números y guiones' };
      }
      return null;
    };
  }

  private fechaCaducidadValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const valor = String(control.value ?? '').trim();
      if (!valor) return null;

      const fecha = this.parsearFechaIso(valor);
      if (!fecha) {
        return { fechaInvalida: 'Fecha de caducidad no válida' };
      }

      const hoy = this.obtenerHoyLocalSinHora();
      if (fecha <= hoy) {
        return { loteCaducado: 'No se puede registrar un lote caducado' };
      }

      return null;
    };
  }

  private cantidadEnteraValidator(): ValidatorFn {
    const regexEntero = /^\d+$/;
    return (control: AbstractControl): ValidationErrors | null => {
      const valor = String(control.value ?? '').trim();
      if (!valor) return null;
      if (!regexEntero.test(valor)) {
        return { cantidadInvalida: 'La cantidad debe ser un número entero de 0 o más' };
      }
      return null;
    };
  }

  private parsearFechaIso(valor: string): Date | null {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(valor);
    if (!match) return null;

    const anio = Number(match[1]);
    const mes = Number(match[2]);
    const dia = Number(match[3]);
    const fecha = new Date(anio, mes - 1, dia);

    if (
      fecha.getFullYear() !== anio ||
      fecha.getMonth() !== mes - 1 ||
      fecha.getDate() !== dia
    ) {
      return null; // Fecha inexistente como 31/02/2027
    }
    return fecha;
  }

  private obtenerHoyLocalSinHora(): Date {
    const ahora = new Date();
    return new Date(ahora.getFullYear(), ahora.getMonth(), ahora.getDate());
  }

  private caducaEnTresMesesOMenos(fechaCaducidad: Date): boolean {
    const limite = this.obtenerHoyLocalSinHora();
    limite.setMonth(limite.getMonth() + 3);
    return fechaCaducidad <= limite;
  }

  private enfocarPrimerCampoInvalido(): void {
    const ordenCampos = [
      'nombreComercial',
      'nombreSustancia',
      'presentacion',
      'concentracion',
      'numeroLote',
      'fechaCaducidad',
      'cantidadInicial',
    ] as const;

    for (const campo of ordenCampos) {
      if (this.formulario.controls[campo].invalid) {
        document.getElementById(campo)?.focus();
        break;
      }
    }
  }

  /** Lee lo capturado en el formulario, sin espacios al inicio ni al final. */
  private construirDatos(): RegistrarMedicamentoRequest {
    const raw = this.formulario.getRawValue();
    return {
      nombreComercial: raw.nombreComercial.trim(),
      nombreSustancia: raw.nombreSustancia.trim(),
      presentacion: raw.presentacion.trim(),
      concentracion: raw.concentracion.trim(),
      esControlado: raw.esControlado,
      numeroLote: raw.numeroLote.trim(),
      fechaCaducidad: raw.fechaCaducidad.trim(),
      cantidadInicial: String(raw.cantidadInicial).trim(),
    };
  }

  private obtenerMensajeError(error: HttpErrorResponse): string {
    // HU-06 escenario 14: medicamento duplicado o regla de dominio (422)
    if (error.status === 422 && error.error?.detail) {
      return error.error.detail;
    }
    // Errores de validación del backend (400)
    if (error.status === 400 && Array.isArray(error.error?.errores) && error.error.errores.length > 0) {
      return error.error.errores.join(' ');
    }
    // HU-06 escenario 17: fallo de red o del servidor
    return 'No se pudo registrar';
  }
}