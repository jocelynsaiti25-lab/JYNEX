import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';

import {
  MedicamentoParaSalida,
  PropuestaSalida,
  SalidaRegistrada,
} from '../../../core/models/salida-medicamento.model';
import { SalidaMedicamentoService } from '../../../core/services/salida-medicamento.service';
import { AlertaComponent } from '../../../shared/components/alerta/alerta.component';
import { ConfirmacionComponent } from '../../../shared/components/confirmacion/confirmacion.component';
import { EstadoSemaforo, SemaforoComponent } from '../../../shared/components/semaforo/semaforo.component';

interface ErrorApi {
  detail?: string;
  errores?: string[];
}

const MENSAJE_OBLIGATORIO = 'Este campo es obligatorio';
const MENSAJE_CANTIDAD = 'La cantidad debe ser un número entero mayor que 0';

export function validarCantidad(texto: string): string | null {
  const limpio = texto.trim();
  if (limpio === '') return MENSAJE_OBLIGATORIO;
  if (!/^\d+$/.test(limpio) || Number(limpio) < 1 || !Number.isSafeInteger(Number(limpio))) return MENSAJE_CANTIDAD;
  return null;
}

function obtenerErrores(error: HttpErrorResponse): string[] {
  const cuerpo = (error.error ?? {}) as ErrorApi;

  if (error.status === 400 && cuerpo.errores?.length) return cuerpo.errores;
  if ((error.status === 404 || error.status === 422) && cuerpo.detail) return [cuerpo.detail];
  if (error.status === 403) return ['No tienes permiso para registrar salidas.'];
  return ['No se pudo registrar la salida'];
}

@Component({
  selector: 'app-salida-medicamentos',
  imports: [AlertaComponent, ConfirmacionComponent, SemaforoComponent],
  templateUrl: './salida-medicamentos.component.html',
  styleUrl: './salida-medicamentos.component.scss',
})
export class SalidaMedicamentosComponent implements OnInit {
  private readonly servicio = inject(SalidaMedicamentoService);

  readonly termino = signal('');
  readonly resultados = signal<MedicamentoParaSalida[]>([]);
  readonly buscando = signal(false);
  readonly errorBusqueda = signal<string | null>(null);

  readonly seleccionado = signal<MedicamentoParaSalida | null>(null);
  readonly cantidad = signal('');
  readonly errorCantidad = signal<string | null>(null);

  readonly calculando = signal(false);
  readonly propuesta = signal<PropuestaSalida | null>(null);
  readonly erroresPropuesta = signal<string[]>([]);

  readonly registrando = signal(false);
  readonly salidaRegistrada = signal<SalidaRegistrada | null>(null);
  readonly erroresRegistro = signal<string[]>([]);

  ngOnInit(): void {
    this.buscar();
  }

  actualizarTermino(valor: string): void {
    this.termino.set(valor);
  }

  buscar(): void {
    this.buscando.set(true);
    this.errorBusqueda.set(null);

    this.servicio.buscar(this.termino()).subscribe({
      next: (datos) => {
        this.resultados.set(datos);
        this.buscando.set(false);
      },
      error: () => {
        this.buscando.set(false);
        this.errorBusqueda.set('No se pudo cargar la lista de medicamentos.');
      },
    });
  }

  seleccionar(medicamento: MedicamentoParaSalida): void {
    this.seleccionado.set(medicamento);
    this.cantidad.set('');
    this.errorCantidad.set(null);
    this.propuesta.set(null);
    this.erroresPropuesta.set([]);
    this.erroresRegistro.set([]);
    this.salidaRegistrada.set(null);
  }

  quitarSeleccion(): void {
    if (this.registrando()) return;
    this.seleccionado.set(null);
    this.propuesta.set(null);
    this.erroresPropuesta.set([]);
  }

  actualizarCantidad(valor: string): void {
    this.cantidad.set(valor);
    this.errorCantidad.set(null);
    this.propuesta.set(null);
    this.erroresPropuesta.set([]);
    this.erroresRegistro.set([]);
  }

  calcularPropuesta(): void {
    const medicamento = this.seleccionado();
    if (!medicamento || this.calculando()) return;

    const error = validarCantidad(this.cantidad());
    this.errorCantidad.set(error);
    if (error) return;

    this.calculando.set(true);
    this.propuesta.set(null);
    this.erroresPropuesta.set([]);
    this.salidaRegistrada.set(null);

    this.servicio.proponer(medicamento.idMedicamento, this.cantidad()).subscribe({
      next: (propuesta) => {
        this.calculando.set(false);
        this.propuesta.set(propuesta);
      },
      error: (respuesta: HttpErrorResponse) => {
        this.calculando.set(false);
        this.erroresPropuesta.set(obtenerErrores(respuesta));
      },
    });
  }

  cancelarPropuesta(): void {
    if (this.registrando()) return;
    this.propuesta.set(null);
  }

  confirmarSalida(): void {
    const medicamento = this.seleccionado();
    if (!medicamento || !this.propuesta() || this.registrando()) return;

    this.registrando.set(true);
    this.erroresRegistro.set([]);

    this.servicio.registrar({ idMedicamento: medicamento.idMedicamento, cantidad: this.cantidad().trim() }).subscribe({
      next: (resultado) => {
        this.registrando.set(false);
        this.propuesta.set(null);
        this.salidaRegistrada.set(resultado);
        this.cantidad.set('');
        this.seleccionado.set(null);
        this.buscar();
      },
      error: (respuesta: HttpErrorResponse) => {
        this.registrando.set(false);
        this.propuesta.set(null);
        this.erroresRegistro.set(obtenerErrores(respuesta));
        this.buscar();
      },
    });
  }

  estadoSemaforo(semaforo: string): EstadoSemaforo {
    switch (semaforo) {
      case 'Rojo':
        return 'rojo';
      case 'Amarillo':
        return 'amarillo';
      default:
        return 'verde';
    }
  }
}
