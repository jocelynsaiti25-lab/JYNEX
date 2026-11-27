import { HttpErrorResponse } from '@angular/common/http';
import { Component, HostListener, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ConCambiosSinGuardar } from '../../../core/guards/cambios-sin-guardar.guard';
import { MedicamentoParaEditar } from '../../../core/models/medicamento.model';
import { MedicamentosService } from '../../../core/services/medicamentos.service';
import { AlertaComponent } from '../../../shared/components/alerta/alerta.component';
import { ConfirmacionComponent } from '../../../shared/components/confirmacion/confirmacion.component';

/** Respuesta de error de la API (ProblemDetails de GlobalExceptionHandler). */
interface ErrorApi {
  detail?: string;
  errores?: string[];
}

type Campo = 'nombreComercial' | 'nombreSustancia' | 'presentacion' | 'concentracion';

/**
 * HU-09: edición de información descriptiva de medicamentos (solo el Administrador: rolGuard en app.routes.ts).
 * El medicamento se elige en el inventario (HU-08): /medicamentos/{idMedicamento}/editar.
 *   Escenario 2:        formulario precargado con los valores actuales.
 *   Escenario 7:        campos obligatorios vacíos → NO abre la confirmación.
 *   Escenarios 5, 6:     confirmación con resumen antes/después; "Cancelar" no cambia nada.
 *   Escenario 1:         "Medicamento actualizado".
 *   Escenario 8:         alerta de duplicado (422), datos anteriores intactos.
 *   Escenario 9:         cambiar solo "Medicamento controlado" o mayúsculas se guarda igual.
 *   Escenario 10:        existencias y semáforo solo de referencia (bloqueados).
 *   Escenario 12:        sin cambios reales, "Guardar" permanece deshabilitado.
 *   Escenarios 13, 14:   aviso al salir con cambios sin guardar (cambiosSinGuardarGuard).
 *   Escenario 15:        alerta de error; no queda nada a medias.
 *   Escenario 16:        botones desactivados mientras se procesa.
 */
@Component({
  selector: 'app-editar-medicamento',
  imports: [ReactiveFormsModule, RouterLink, AlertaComponent, ConfirmacionComponent],
  templateUrl: './editar-medicamento.component.html',
  styleUrl: './editar-medicamento.component.scss',
})
export class EditarMedicamentoComponent implements OnInit, ConCambiosSinGuardar {
  private readonly medicamentosService = inject(MedicamentosService);
  private readonly fb = inject(FormBuilder);

  /** Viene de la dirección /medicamentos/:idMedicamento/editar. */
  private readonly idMedicamento = Number(inject(ActivatedRoute).snapshot.paramMap.get('idMedicamento'));

  readonly medicamento = signal<MedicamentoParaEditar | null>(null);
  readonly cargando = signal(true);
  readonly errorCarga = signal<string | null>(null);

  readonly confirmando = signal(false);
  readonly guardando = signal(false);
  readonly mensajeExito = signal<string | null>(null);
  readonly errores = signal<string[]>([]);

  readonly formulario = this.fb.nonNullable.group({
    nombreComercial: ['', [Validators.required, Validators.maxLength(100)]],
    nombreSustancia: ['', [Validators.required, Validators.maxLength(100)]],
    presentacion: ['', [Validators.required, Validators.maxLength(50)]],
    concentracion: ['', [Validators.required, Validators.maxLength(50)]],
    esControlado: [false],
  });

  ngOnInit(): void {
    this.cargarMedicamento();
  }

  /** HU-09 escenario 13: advierte si intenta cerrar o recargar la pestaña con cambios sin guardar. */
  @HostListener('window:beforeunload', ['$event'])
  alCerrarPestana(event: BeforeUnloadEvent): void {
    if (this.hayCambiosSinGuardar() && !this.guardando()) {
      event.preventDefault();
    }
  }

  /** Usado por cambiosSinGuardarGuard al navegar dentro de la app o cerrar sesión (escenarios 13 y 14). */
  puedeSalir(): boolean {
    if (!this.hayCambiosSinGuardar() || this.guardando()) {
      return true;
    }
    return window.confirm('Tienes cambios sin guardar. Si sales ahora, se perderán. ¿Deseas salir de todos modos?');
  }

  cargarMedicamento(): void {
    this.cargando.set(true);
    this.errorCarga.set(null);

    this.medicamentosService.obtenerParaEditar(this.idMedicamento).subscribe({
      next: (medicamento) => {
        this.medicamento.set(medicamento);
        this.formulario.reset({
          nombreComercial: medicamento.nombreComercial,
          nombreSustancia: medicamento.nombreSustancia,
          presentacion: medicamento.presentacion,
          concentracion: medicamento.concentracion,
          esControlado: medicamento.esControlado,
        });
        this.cargando.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.cargando.set(false);
        this.errorCarga.set(
          error.status === 404 ? 'El medicamento no existe' : 'No se pudo cargar el medicamento. Revisa tu conexión e intenta de nuevo.'
        );
      },
    });
  }

  /** HU-09 escenario 12: solo hay algo que guardar si algún valor real cambió. */
  hayCambiosSinGuardar(): boolean {
    const original = this.medicamento();
    if (!original) return false;

    const actual = this.formulario.getRawValue();
    return (
      actual.nombreComercial.trim() !== original.nombreComercial ||
      actual.nombreSustancia.trim() !== original.nombreSustancia ||
      actual.presentacion.trim() !== original.presentacion ||
      actual.concentracion.trim() !== original.concentracion ||
      actual.esControlado !== original.esControlado
    );
  }

  /** HU-09 escenario 7: si hay errores no se abre la confirmación. */
  guardar(): void {
    this.mensajeExito.set(null);
    this.errores.set([]);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      const primero: Campo | undefined = (
        ['nombreComercial', 'nombreSustancia', 'presentacion', 'concentracion'] as Campo[]
      ).find((campo) => this.formulario.controls[campo].invalid);
      if (primero) document.getElementById(primero)?.focus();
      return;
    }

    if (!this.hayCambiosSinGuardar()) {
      return; // Escenario 12: nada que confirmar
    }

    this.confirmando.set(true); // Escenario 5
  }

  /** Escenario 1, 8 y 16: confirma y envía la petición una sola vez. */
  confirmarEdicion(): void {
    const original = this.medicamento();
    if (!original || this.guardando()) return;

    this.guardando.set(true);

    const datos = this.formulario.getRawValue();
    this.medicamentosService
      .editar(original.idMedicamento, {
        nombreComercial: datos.nombreComercial.trim(),
        nombreSustancia: datos.nombreSustancia.trim(),
        presentacion: datos.presentacion.trim(),
        concentracion: datos.concentracion.trim(),
        esControlado: datos.esControlado,
      })
      .subscribe({
        next: (resultado) => {
          this.guardando.set(false);
          this.confirmando.set(false);
          this.medicamento.set({
            ...original,
            nombreComercial: resultado.nombreComercial,
            nombreSustancia: resultado.nombreSustancia,
            presentacion: resultado.presentacion,
            concentracion: resultado.concentracion,
            esControlado: resultado.esControlado,
          });
          this.formulario.markAsPristine();
          this.mensajeExito.set(resultado.mensaje || 'Medicamento actualizado'); // Escenario 1
        },
        error: (error: HttpErrorResponse) => {
          this.guardando.set(false);
          this.confirmando.set(false);
          this.errores.set(obtenerErrores(error)); // Escenarios 8 y 15
        },
      });
  }

  cancelarConfirmacion(): void {
    if (this.guardando()) return;
    this.confirmando.set(false); // Escenario 6: conserva lo capturado
  }

  campoInvalido(campo: Campo): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && control.touched;
  }
}

/** Mensajes de error de la API. */
function obtenerErrores(error: HttpErrorResponse): string[] {
  const cuerpo = (error.error ?? {}) as ErrorApi;

  if (error.status === 400 && cuerpo.errores?.length) {
    return cuerpo.errores;
  }
  if ((error.status === 404 || error.status === 422) && cuerpo.detail) {
    return [cuerpo.detail]; // "El medicamento no existe" / duplicado en el catálogo
  }
  if (error.status === 403) {
    return ['No tienes permiso para editar medicamentos.'];
  }
  // Escenario 15: el servidor no respondió o falló; no quedó ningún cambio a medias
  return ['No se pudo actualizar el medicamento. Revisa tu conexión e intenta de nuevo.'];
}
