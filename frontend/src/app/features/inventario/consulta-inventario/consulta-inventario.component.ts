import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { ItemInventario } from '../../../core/models/inventario.model';
import { InventarioService } from '../../../core/services/inventario.service';

export type FiltroSemaforo = 'Todos' | 'Rojo' | 'Amarillo' | 'Verde';

@Component({
  selector: 'app-consulta-inventario',
  standalone: true,
  templateUrl: './consulta-inventario.component.html',
  styleUrl: './consulta-inventario.component.scss',
})
export class ConsultaInventarioComponent implements OnInit {
  private readonly inventarioService = inject(InventarioService);

  // Estado reactivo de datos y filtros (HU-08 escenarios 1, 5 y 7)
  readonly itemsInventario = signal<ItemInventario[]>([]);
  readonly cargando = signal(false);
  readonly errorCarga = signal<string | null>(null);

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

  ngOnInit(): void {
    this.cargarInventario();
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