import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ResultadoBaja } from '../../../core/models/baja-lote.model';
import { ItemInventario } from '../../../core/models/inventario.model';
import { InventarioService } from '../../../core/services/inventario.service';
import { SesionService } from '../../../core/services/sesion.service';
import { AlertaComponent } from '../../../shared/components/alerta/alerta.component';
import { EstadoSemaforo, SemaforoComponent } from '../../../shared/components/semaforo/semaforo.component';
import { BajaLoteComponent, puedeDarseDeBaja } from '../../baja-lotes/baja-lote.component';

export type FiltroSemaforo = 'Todos' | 'Rojo' | 'Amarillo' | 'Verde';

const ETIQUETAS_FILTRO: Record<FiltroSemaforo, string> = {
  Todos: 'Todos',
  Rojo: 'Rojo',
  Amarillo: 'Amarillo',
  Verde: 'Verde',
};

@Component({
  selector: 'app-consulta-inventario',
  standalone: true,
  imports: [RouterLink, SemaforoComponent, AlertaComponent, BajaLoteComponent],
  templateUrl: './consulta-inventario.component.html',
  styleUrl: './consulta-inventario.component.scss',
})
export class ConsultaInventarioComponent implements OnInit {
  private readonly inventarioService = inject(InventarioService);
  private readonly sesion = inject(SesionService);

  // HU-09 escenarios 2 y 3: el botón "Editar" solo lo ve el Administrador (la API también lo revisa)
  readonly esAdministrador = computed(() => this.sesion.usuario()?.rol === 'Administrador');

  // Estado reactivo de datos y filtros (HU-08 escenarios 1, 5 y 7)
  readonly itemsInventario = signal<ItemInventario[]>([]);
  readonly cargando = signal(false);
  readonly errorCarga = signal<string | null>(null);

  readonly loteEnBaja = signal<ItemInventario | null>(null);
  readonly resultadoBaja = signal<ResultadoBaja | null>(null);
  readonly puedeDarseDeBaja = puedeDarseDeBaja;

  readonly terminoBusqueda = signal('');
  readonly filtroSemaforo = signal<FiltroSemaforo>('Todos');

  /**
   * HU-08 escenarios 1, 2, 4, 5 y 7:
   * Filtra dinámicamente en la interfaz web conforme el usuario escribe o cambia el semáforo,
   * sin recargar la página y conservando ambos criterios combinados.
   */
  readonly resultadosFiltrados = computed(() => {
    const termino = this.terminoBusqueda().trim().toLowerCase();
    const semaforo = this.filtroSemaforo();

    return this.itemsInventario().filter((item) => {
      const cumpleSemaforo =
        semaforo === 'Todos' || item.semaforoCaducidad.toLowerCase() === semaforo.toLowerCase();

      if (!cumpleSemaforo) return false;
      if (!termino) return true;

      return (
        item.nombreComercial.toLowerCase().includes(termino) ||
        item.nombreSustancia.toLowerCase().includes(termino) ||
        item.numeroLote.toLowerCase().includes(termino)
      );
    });
  });

  /**
   * HU-10 escenario 14: mensaje claro cuando el filtro de semáforo (combinado o no con la
   * búsqueda) no encuentra ningún lote, sin generar errores en pantalla.
   */
  readonly mensajeSinResultados = computed(() => {
    const semaforo = this.filtroSemaforo();
    const termino = this.terminoBusqueda().trim();

    if (semaforo === 'Todos') {
      return termino
        ? 'No hay medicamentos ni lotes que coincidan con la búsqueda.'
        : 'No hay medicamentos registrados en el inventario.';
    }

    return termino
      ? `No hay lotes en la categoría "${ETIQUETAS_FILTRO[semaforo]}" que coincidan con la búsqueda.`
      : `No hay lotes registrados en la categoría "${ETIQUETAS_FILTRO[semaforo]}".`;
  });

  ngOnInit(): void {
    this.cargarInventario();
  }

  /** HU-10 escenario 5: estado visual para <app-semaforo>, priorizando la marca de caducado. */
  estadoSemaforo(item: ItemInventario): EstadoSemaforo | null {
    if (item.semaforoCaducidad === 'SinStock') return null;
    if (item.estaCaducado) return 'caducado';
    return item.semaforoCaducidad.toLowerCase() as EstadoSemaforo;
  }

  cargarInventario(): void {
    this.cargando.set(true);
    this.errorCarga.set(null);

    this.inventarioService.consultar().subscribe({
      next: (datos) => {
        this.itemsInventario.set(datos);
        this.cargando.set(false);
      },
      error: () => {
        this.cargando.set(false);
        this.errorCarga.set('No se pudo cargar el inventario de medicamentos.');
      },
    });
  }

  iniciarBaja(item: ItemInventario): void {
    this.resultadoBaja.set(null);
    this.loteEnBaja.set(item);
  }

  terminarBaja(resultado: ResultadoBaja): void {
    this.loteEnBaja.set(null);
    this.resultadoBaja.set(resultado);
    this.cargarInventario();
  }

  cancelarBaja(): void {
    this.loteEnBaja.set(null);
  }

  actualizarBusqueda(valor: string): void {
    this.terminoBusqueda.set(valor);
  }

  limpiarBusqueda(): void {
    this.terminoBusqueda.set('');
  }

  cambiarFiltroSemaforo(filtro: FiltroSemaforo): void {
    this.filtroSemaforo.set(filtro);
  }
}