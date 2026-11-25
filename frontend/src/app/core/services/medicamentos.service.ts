import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  RegistrarMedicamentoRequest,
  RegistrarMedicamentoResponse,
} from '../models/medicamento.model';

/** Servicio HTTP para operaciones del catálogo de medicamentos (HU-06). */
@Injectable({ providedIn: 'root' })
export class MedicamentosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/medicamentos`;

  registrar(datos: RegistrarMedicamentoRequest): Observable<RegistrarMedicamentoResponse> {
    return this.http.post<RegistrarMedicamentoResponse>(this.apiUrl, datos);
  }
}