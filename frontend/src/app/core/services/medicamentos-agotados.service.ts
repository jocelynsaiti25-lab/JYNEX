import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { MedicamentoAgotado } from '../models/medicamento-agotado.model';

/** Servicio HTTP para la consulta de medicamentos agotados (HU-12). */
@Injectable({ providedIn: 'root' })
export class MedicamentosAgotadosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/medicamentos/agotados`;

  obtenerAgotados(): Observable<MedicamentoAgotado[]> {
    return this.http.get<MedicamentoAgotado[]>(this.apiUrl);
  }
}
