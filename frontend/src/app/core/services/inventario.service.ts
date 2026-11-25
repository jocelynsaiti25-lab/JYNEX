import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ItemInventario } from '../models/inventario.model';

/** Servicio HTTP para búsqueda y consulta del inventario, incluido el semáforo de caducidad (HU-08, HU-10). */
@Injectable({ providedIn: 'root' })
export class InventarioService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/inventario`;

  consultar(termino = '', semaforo = 'Todos'): Observable<ItemInventario[]> {
    let params = new HttpParams();
    if (termino.trim()) {
      params = params.set('termino', termino.trim());
    }
    if (semaforo && semaforo !== 'Todos') {
      params = params.set('semaforo', semaforo);
    }
    return this.http.get<ItemInventario[]>(this.apiUrl, { params });
  }
}