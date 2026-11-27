import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { FiltroBitacora, MedicamentoBitacora, PaginaBitacora } from '../models/bitacora-movimientos.model';

@Injectable({ providedIn: 'root' })
export class BitacoraMovimientosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/bitacora`;

  consultar(filtro: FiltroBitacora, pagina: number, tamanoPagina: number): Observable<PaginaBitacora> {
    let params = new HttpParams().set('pagina', pagina).set('tamanoPagina', tamanoPagina);

    if (filtro.desde) {
      params = params.set('desde', filtro.desde);
    }
    if (filtro.hasta) {
      params = params.set('hasta', filtro.hasta);
    }
    if (filtro.idUsuario) {
      params = params.set('idUsuario', filtro.idUsuario);
    }
    if (filtro.idMedicamento) {
      params = params.set('idMedicamento', filtro.idMedicamento);
    }
    if (filtro.tipo) {
      params = params.set('tipo', filtro.tipo);
    }

    return this.http.get<PaginaBitacora>(`${this.url}/movimientos`, { params });
  }

  medicamentos(): Observable<MedicamentoBitacora[]> {
    return this.http.get<MedicamentoBitacora[]>(`${this.url}/medicamentos`);
  }
}
