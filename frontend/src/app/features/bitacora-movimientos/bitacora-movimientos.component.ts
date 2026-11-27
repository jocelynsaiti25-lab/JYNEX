import { DOCUMENT, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import {
  FiltroBitacora,
  MedicamentoBitacora,
  MovimientoBitacora,
  PaginaBitacora,
  TIPOS_MOVIMIENTO,
  TipoMovimiento,
} from '../../core/models/bitacora-movimientos.model';
import { UsuarioRegistrado } from '../../core/models/usuario.model';
import { BitacoraMovimientosService } from '../../core/services/bitacora-movimientos.service';
import { UsuariosService } from '../../core/services/usuarios.service';
import { AlertaComponent } from '../../shared/components/alerta/alerta.component';
import { ConfirmacionComponent } from '../../shared/components/confirmacion/confirmacion.component';

const TAMANO_PAGINA = 20;
const MENSAJE_RANGO_NO_VALIDO = 'El rango de fechas no es válido.';
const MENSAJE_SIN_MOVIMIENTOS = 'No hay movimientos para exportar.';

@Component({
  selector: 'app-bitacora-movimientos',
  imports: [ReactiveFormsModule, DatePipe, AlertaComponent, ConfirmacionComponent],
  templateUrl: './bitacora-movimientos.component.html',
  styleUrl: './bitacora-movimientos.component.scss',
})
export class BitacoraMovimientosComponent implements OnInit {
  private readonly bitacora = inject(BitacoraMovimientosService);
  private readonly usuariosService = inject(UsuariosService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly documento = inject(DOCUMENT);

  readonly tipos = TIPOS_MOVIMIENTO;

  readonly filtros = new FormGroup({
    desde: new FormControl('', { nonNullable: true }),
    hasta: new FormControl('', { nonNullable: true }),
    idUsuario: new FormControl<number | null>(null),
    idMedicamento: new FormControl<number | null>(null),
    tipo: new FormControl<TipoMovimiento | ''>('', { nonNullable: true }),
  });

  readonly usuarios = signal<UsuarioRegistrado[]>([]);
  readonly medicamentos = signal<MedicamentoBitacora[]>([]);
  readonly resultado = signal<PaginaBitacora | null>(null);
  readonly cargando = signal(true);
  readonly error = signal(false);
  readonly errorRango = signal<string | null>(null);
  readonly conFiltros = signal(false);

  readonly confirmandoExportacion = signal(false);
  readonly exportando = signal(false);
  readonly avisoExportacion = signal<string | null>(null);
  readonly errorExportacion = signal(false);
  readonly descargaIniciada = signal(false);
  private readonly filtroVigente = signal<FiltroBitacora>({});

  readonly totalAExportar = computed(() => this.resultado()?.total ?? 0);

  readonly resumenFiltros = computed(() => {
    const filtro = this.filtroVigente();
    const usuario = this.usuarios().find((u) => u.idUsuario === filtro.idUsuario);
    const medicamento = this.medicamentos().find((m) => m.idMedicamento === filtro.idMedicamento);

    return [
      { etiqueta: 'Rango de fechas', valor: this.describirRango(filtro) },
      { etiqueta: 'Usuario', valor: usuario ? `${usuario.nombreCompleto} (${usuario.nombreUsuario})` : 'Todos' },
      { etiqueta: 'Medicamento', valor: medicamento ? this.nombreMedicamento(medicamento) : 'Todos' },
      { etiqueta: 'Tipo de movimiento', valor: filtro.tipo ?? 'Todos' },
    ];
  });

  private filtroAplicado: FiltroBitacora = {};
  private paginaActual = 1;
  private solicitud?: Subscription;

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => this.solicitud?.unsubscribe());

    this.usuariosService
      .consultarUsuarios()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (lista) => this.usuarios.set(lista), error: () => this.usuarios.set([]) });

    this.bitacora
      .medicamentos()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (lista) => this.medicamentos.set(lista), error: () => this.medicamentos.set([]) });

    this.cargar();
  }

  aplicarFiltros(): void {
    const { desde, hasta } = this.filtros.getRawValue();

    if (desde && hasta && desde > hasta) {
      this.errorRango.set(MENSAJE_RANGO_NO_VALIDO);
      return;
    }

    this.errorRango.set(null);
    this.filtroAplicado = this.leerFiltros();
    this.filtroVigente.set(this.filtroAplicado);
    this.conFiltros.set(this.hayFiltros(this.filtroAplicado));
    this.paginaActual = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.filtros.reset({ desde: '', hasta: '', idUsuario: null, idMedicamento: null, tipo: '' });
    this.errorRango.set(null);
    this.filtroAplicado = {};
    this.filtroVigente.set({});
    this.conFiltros.set(false);
    this.paginaActual = 1;
    this.cargar();
  }

  actualizar(): void {
    this.cargar();
  }

  irAPagina(pagina: number): void {
    const total = this.resultado()?.totalPaginas ?? 1;
    if (pagina < 1 || pagina > total) {
      return;
    }

    this.paginaActual = pagina;
    this.cargar();
  }

  intentarDeNuevo(): void {
    this.cargar();
  }

  exportar(): void {
    this.descargaIniciada.set(false);
    this.errorExportacion.set(false);
    this.avisoExportacion.set(null);

    if (this.totalAExportar() === 0) {
      this.avisoExportacion.set(MENSAJE_SIN_MOVIMIENTOS);
      return;
    }

    this.confirmandoExportacion.set(true);
  }

  cancelarExportacion(): void {
    if (this.exportando()) {
      return;
    }

    this.confirmandoExportacion.set(false);
  }

  confirmarExportacion(): void {
    if (this.exportando()) {
      return;
    }

    this.exportando.set(true);

    this.bitacora
      .exportar(this.filtroAplicado)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (respuesta) => {
          const archivo = respuesta.body;
          if (!archivo || archivo.size === 0) {
            this.terminarConError();
            return;
          }

          this.descargar(archivo, this.nombreDeArchivo(respuesta.headers.get('content-disposition')));
          this.exportando.set(false);
          this.confirmandoExportacion.set(false);
          this.descargaIniciada.set(true);
        },
        error: (fallo: HttpErrorResponse) => {
          if (fallo.status === 422) {
            this.exportando.set(false);
            this.confirmandoExportacion.set(false);
            this.avisoExportacion.set(MENSAJE_SIN_MOVIMIENTOS);
            return;
          }

          this.terminarConError();
        },
      });
  }

  cerrarAvisoExportacion(): void {
    this.avisoExportacion.set(null);
    this.descargaIniciada.set(false);
  }

  nombreMedicamento(m: MedicamentoBitacora): string {
    return `${m.nombreComercial} · ${m.presentacion} · ${m.concentracion}`;
  }

  descripcionMedicamento(movimiento: MovimientoBitacora): string {
    return `${movimiento.presentacion} · ${movimiento.concentracion}`;
  }

  private terminarConError(): void {
    this.exportando.set(false);
    this.confirmandoExportacion.set(false);
    this.errorExportacion.set(true);
  }

  private describirRango(filtro: FiltroBitacora): string {
    if (filtro.desde && filtro.hasta) {
      return `Del ${this.formatearDia(filtro.desde)} al ${this.formatearDia(filtro.hasta)}`;
    }
    if (filtro.desde) {
      return `Desde el ${this.formatearDia(filtro.desde)}`;
    }
    if (filtro.hasta) {
      return `Hasta el ${this.formatearDia(filtro.hasta)}`;
    }
    return 'Todas las fechas';
  }

  private formatearDia(dia: string): string {
    const [anio, mes, diaDelMes] = dia.split('-');
    return `${diaDelMes}/${mes}/${anio}`;
  }

  private nombreDeArchivo(contentDisposition: string | null): string {
    const coincidencia = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(contentDisposition ?? '');
    if (coincidencia) {
      return decodeURIComponent(coincidencia[1]);
    }

    return `bitacora-movimientos-${new Date().toISOString().slice(0, 10)}.xlsx`;
  }

  private descargar(archivo: Blob, nombre: string): void {
    const direccion = URL.createObjectURL(archivo);
    const enlace = this.documento.createElement('a');
    enlace.href = direccion;
    enlace.download = nombre;
    this.documento.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    URL.revokeObjectURL(direccion);
  }

  private leerFiltros(): FiltroBitacora {
    const valores = this.filtros.getRawValue();

    return {
      desde: valores.desde || undefined,
      hasta: valores.hasta || undefined,
      idUsuario: valores.idUsuario ?? undefined,
      idMedicamento: valores.idMedicamento ?? undefined,
      tipo: valores.tipo || undefined,
    };
  }

  private hayFiltros(filtro: FiltroBitacora): boolean {
    return Object.values(filtro).some((valor) => valor !== undefined);
  }

  private cargar(): void {
    this.solicitud?.unsubscribe();
    this.cargando.set(true);
    this.error.set(false);

    this.solicitud = this.bitacora
      .consultar(this.filtroAplicado, this.paginaActual, TAMANO_PAGINA)
      .subscribe({
        next: (pagina) => {
          this.resultado.set(pagina);
          this.cargando.set(false);
        },
        error: () => {
          this.error.set(true);
          this.cargando.set(false);
        },
      });
  }
}
