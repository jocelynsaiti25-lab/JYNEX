import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  MedicamentoActivo,
  RegistrarEntradaLoteRequest,
  RegistrarEntradaLoteResponse,
} from '../models/entrada-lote.model';

/** Servicio HTTP para búsqueda de medicamentos activos y registro de entrada de lote (HU-07). */
@Injectable({ providedIn: 'root' })
export class EntradaLoteService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/entradas-lote`;

  buscarMedicamentosActivos(termino = ''): Observable<MedicamentoActivo[]> {
    const params = new HttpParams().set('termino', termino.trim());
    return this.http.get<MedicamentoActivo[]>(`${this.apiUrl}/medicamentos`, { params });
  }

  registrar(datos: RegistrarEntradaLoteRequest): Observable<RegistrarEntradaLoteResponse> {
    return this.http.post<RegistrarEntradaLoteResponse>(this.apiUrl, datos);
  }
}