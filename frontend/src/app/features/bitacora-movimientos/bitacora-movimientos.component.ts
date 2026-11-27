import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, of, switchMap, tap } from 'rxjs';

import { FiltrosBitacora, MedicamentoBitacora, MovimientoBitacora, PaginaBitacora } from '../../core/models/bitacora-movimientos.model';
import { UsuarioRegistrado } from '../../core/models/usuario.model';
import { BitacoraMovimientosService } from '../../core/services/bitacora-movimientos.service';
import { UsuariosService } from '../../core/services/usuarios.service';
import { AlertaComponent } from '../../shared/components/alerta/alerta.component';

interface FiltrosFormulario {
  desde: string;
  hasta: string;
  idUsuario: string;
  idMedicamento: string;
  tipo: string;
}

const TAMANO_PAGINA = 20;

/** HU-15: historial de solo lectura, accesible desde una ruta restringida a Administrador. */
@Component({
  selector: 'app-bitacora-movimientos',
  imports: [DatePipe, AlertaComponent],
  templateUrl: './bitacora-movimientos.component.html',
  styleUrl: './bitacora-movimientos.component.scss',
})
export class BitacoraMovimientosComponent implements OnInit {
  private readonly bitacoraService = inject(BitacoraMovimientosService);
  private readonly usuariosService = inject(UsuariosService);
  private readonly solicitarConsulta = new Subject<{ filtros: FiltrosBitacora; filtrosActivos: boolean }>();

  readonly usuarios = signal<UsuarioRegistrado[]>([]);
  readonly medicamentos = signal<MedicamentoBitacora[]>([]);
  readonly movimientos = signal<MovimientoBitacora[]>([]);
  readonly cargando = signal(true);
  readonly errorCarga = signal(false);
  readonly errorFiltros = signal<string | null>(null);
  readonly errorOpciones = signal(false);
  readonly cargandoOpciones = signal(true);
  readonly paginaActual = signal(1);
  readonly tamanoPagina = TAMANO_PAGINA;
  readonly totalMovimientos = signal(0);
  readonly totalPaginas = signal(0);
  readonly consultaConFiltros = signal(false);

  readonly desde = signal('');
  readonly hasta = signal('');
  readonly idUsuario = signal('');
  readonly idMedicamento = signal('');
  readonly tipo = signal('');
  private readonly filtrosAplicados = signal<FiltrosFormulario>({
    desde: '',
    hasta: '',
    idUsuario: '',
    idMedicamento: '',
    tipo: '',
  });

  readonly hayFiltrosAplicados = computed(() => this.consultaConFiltros());
  readonly nombrePagina = computed(() => `${this.paginaActual()} de ${this.totalPaginas()}`);

  constructor() {
    this.solicitarConsulta
      .pipe(
        tap(({ filtros, filtrosActivos }) => {
          this.cargando.set(true);
          this.errorCarga.set(false);
          this.movimientos.set([]);
          this.paginaActual.set(filtros.pagina);
          this.consultaConFiltros.set(filtrosActivos);
        }),
        switchMap(({ filtros }) =>
          this.bitacoraService.consultar(filtros).pipe(
            catchError(() => of(null)),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((pagina) => this.recibirPagina(pagina));
  }

  ngOnInit(): void {
    this.cargarOpciones();
    this.consultar(1, this.construirFiltrosFormulario());
  }

  aplicarFiltros(): void {
    const formulario = this.construirFiltrosFormulario();
    if (formulario.desde && formulario.hasta && formulario.desde > formulario.hasta) {
      this.errorFiltros.set('La fecha inicial no puede ser posterior a la fecha final.');
      this.errorCarga.set(false);
      this.movimientos.set([]);
      this.totalMovimientos.set(0);
      this.totalPaginas.set(0);
      this.paginaActual.set(1);
      return;
    }

    this.errorFiltros.set(null);
    this.filtrosAplicados.set(formulario);
    this.consultar(1, formulario);
  }

  limpiarFiltros(): void {
    this.desde.set('');
    this.hasta.set('');
    this.idUsuario.set('');
    this.idMedicamento.set('');
    this.tipo.set('');
    this.errorFiltros.set(null);
    const formulario = this.construirFiltrosFormulario();
    this.filtrosAplicados.set(formulario);
    this.consultar(1, formulario);
  }

  irAPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas() || pagina === this.paginaActual()) return;
    this.consultar(pagina, this.filtrosAplicados());
  }

  reintentar(): void {
    this.consultar(this.paginaActual(), this.filtrosAplicados());
  }

  reintentarOpciones(): void {
    this.cargarOpciones();
  }

  actualizarDesde(valor: string): void {
    this.desde.set(valor);
  }

  actualizarHasta(valor: string): void {
    this.hasta.set(valor);
  }

  actualizarUsuario(valor: string): void {
    this.idUsuario.set(valor);
  }

  actualizarMedicamento(valor: string): void {
    this.idMedicamento.set(valor);
  }

  actualizarTipo(valor: string): void {
    this.tipo.set(valor);
  }

  mensajeSinResultados(): string {
    return this.hayFiltrosAplicados()
      ? 'No se encontraron movimientos con los filtros seleccionados.'
      : 'Todavía no existen movimientos registrados en la bitácora.';
  }

  detallesAccesibles(movimiento: MovimientoBitacora): string {
    return movimiento.detalles
      .map((detalle) => `Lote ${detalle.numeroLote}, cantidad ${detalle.cantidad}`)
      .join('; ');
  }

  private cargarOpciones(): void {
    this.cargandoOpciones.set(true);
    this.errorOpciones.set(false);
    let respuestasPendientes = 2;
    const terminarCarga = () => {
      respuestasPendientes -= 1;
      if (respuestasPendientes === 0) this.cargandoOpciones.set(false);
    };

    this.usuariosService.consultarUsuarios().subscribe({
      next: (usuarios) => {
        this.usuarios.set(usuarios);
        terminarCarga();
      },
      error: () => {
        this.errorOpciones.set(true);
        terminarCarga();
      },
    });

    this.bitacoraService.consultarMedicamentos().subscribe({
      next: (items) => {
        this.medicamentos.set(items);
        terminarCarga();
      },
      error: () => {
        this.errorOpciones.set(true);
        terminarCarga();
      },
    });
  }

  private construirFiltrosFormulario(): FiltrosFormulario {
    return {
      desde: this.desde(),
      hasta: this.hasta(),
      idUsuario: this.idUsuario(),
      idMedicamento: this.idMedicamento(),
      tipo: this.tipo(),
    };
  }

  private consultar(pagina: number, formulario: FiltrosFormulario): void {
    const filtros: FiltrosBitacora = {
      pagina,
      tamanoPagina: this.tamanoPagina,
      ...(formulario.desde ? { desde: formulario.desde } : {}),
      ...(formulario.hasta ? { hasta: formulario.hasta } : {}),
      ...(formulario.idUsuario ? { idUsuario: Number(formulario.idUsuario) } : {}),
      ...(formulario.idMedicamento ? { idMedicamento: Number(formulario.idMedicamento) } : {}),
      ...(formulario.tipo ? { tipo: formulario.tipo as MovimientoBitacora['tipo'] } : {}),
    };

    this.solicitarConsulta.next({
      filtros,
      filtrosActivos: Boolean(
        formulario.desde || formulario.hasta || formulario.idUsuario || formulario.idMedicamento || formulario.tipo,
      ),
    });
  }

  private recibirPagina(pagina: PaginaBitacora | null): void {
    this.cargando.set(false);
    if (pagina === null) {
      this.errorCarga.set(true);
      this.movimientos.set([]);
      this.totalMovimientos.set(0);
      this.totalPaginas.set(0);
      return;
    }

    this.errorCarga.set(false);
    this.movimientos.set(pagina.movimientos);
    this.paginaActual.set(pagina.pagina);
    this.totalMovimientos.set(pagina.totalMovimientos);
    this.totalPaginas.set(pagina.totalPaginas);
  }
}
