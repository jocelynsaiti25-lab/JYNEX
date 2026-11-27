import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { FiltroBitacora, MedicamentoBitacora, PaginaBitacora } from '../models/bitacora-movimientos.model';

@Injectable({ providedIn: 'root' })
export class BitacoraMovimientosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/bitacora`;

  consultar(filtro: FiltroBitacora, pagina: number, tamanoPagina: number): Observable<PaginaBitacora> {
    const params = this.parametros(filtro).set('pagina', pagina).set('tamanoPagina', tamanoPagina);
    return this.http.get<PaginaBitacora>(`${this.url}/movimientos`, { params });
  }

  exportar(filtro: FiltroBitacora): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.url}/movimientos/exportacion`, {
      params: this.parametros(filtro),
      observe: 'response',
      responseType: 'blob',
    });
  }

  medicamentos(): Observable<MedicamentoBitacora[]> {
    return this.http.get<MedicamentoBitacora[]>(`${this.url}/medicamentos`);
  }

  private parametros(filtro: FiltroBitacora): HttpParams {
    let params = new HttpParams();

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

    return params;
  }
}
