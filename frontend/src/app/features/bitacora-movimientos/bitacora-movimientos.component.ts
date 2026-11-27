import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
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

const TAMANO_PAGINA = 20;
const MENSAJE_RANGO_NO_VALIDO = 'El rango de fechas no es válido.';

@Component({
  selector: 'app-bitacora-movimientos',
  imports: [ReactiveFormsModule, DatePipe, AlertaComponent],
  templateUrl: './bitacora-movimientos.component.html',
  styleUrl: './bitacora-movimientos.component.scss',
})
export class BitacoraMovimientosComponent implements OnInit {
  private readonly bitacora = inject(BitacoraMovimientosService);
  private readonly usuariosService = inject(UsuariosService);
  private readonly destroyRef = inject(DestroyRef);

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
    this.conFiltros.set(this.hayFiltros(this.filtroAplicado));
    this.paginaActual = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.filtros.reset({ desde: '', hasta: '', idUsuario: null, idMedicamento: null, tipo: '' });
    this.errorRango.set(null);
    this.filtroAplicado = {};
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

  nombreMedicamento(m: MedicamentoBitacora): string {
    return `${m.nombreComercial} · ${m.presentacion} · ${m.concentracion}`;
  }

  descripcionMedicamento(movimiento: MovimientoBitacora): string {
    return `${movimiento.presentacion} · ${movimiento.concentracion}`;
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
