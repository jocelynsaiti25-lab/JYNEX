import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DarDeBajaLoteRequest, LoteDadoDeBaja } from '../models/baja-lote.model';

@Injectable({ providedIn: 'root' })
export class BajaLotesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/inventario/lotes`;

  darDeBaja(idLote: number, datos: DarDeBajaLoteRequest): Observable<LoteDadoDeBaja> {
    return this.http.post<LoteDadoDeBaja>(`${this.apiUrl}/${idLote}/baja`, datos);
  }
}
