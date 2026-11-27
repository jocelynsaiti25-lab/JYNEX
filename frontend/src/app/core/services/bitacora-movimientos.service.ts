import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { FiltrosBitacora, MedicamentoBitacora, PaginaBitacora } from '../models/bitacora-movimientos.model';

/** HU-15: consulta de solo lectura a GET api/bitacora/movimientos. */
@Injectable({ providedIn: 'root' })
export class BitacoraMovimientosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/bitacora/movimientos`;

  consultar(filtros: FiltrosBitacora): Observable<PaginaBitacora> {
    let params = new HttpParams()
      .set('pagina', filtros.pagina)
      .set('tamanoPagina', filtros.tamanoPagina);

    if (filtros.desde) params = params.set('desde', filtros.desde);
    if (filtros.hasta) params = params.set('hasta', filtros.hasta);
    if (filtros.idUsuario !== undefined) params = params.set('idUsuario', filtros.idUsuario);
    if (filtros.idMedicamento !== undefined) params = params.set('idMedicamento', filtros.idMedicamento);
    if (filtros.tipo) params = params.set('tipo', filtros.tipo);

    return this.http.get<PaginaBitacora>(this.url, { params });
  }

  consultarMedicamentos(): Observable<MedicamentoBitacora[]> {
    return this.http.get<MedicamentoBitacora[]>(`${this.url}/medicamentos`);
  }
}
