import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ActualizarStockMinimoRequest,
  ActualizarStockMinimoResponse,
  MedicamentoStock,
} from '../models/stock-minimo.model';

/** Servicio HTTP para el control de stock mínimo del catálogo (HU-11). */
@Injectable({ providedIn: 'root' })
export class StockMinimoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/medicamentos`;

  /** HU-11 escenario 1: lista de medicamentos para la pantalla de control. */
  obtenerParaControl(): Observable<MedicamentoStock[]> {
    return this.http.get<MedicamentoStock[]>(`${this.apiUrl}/stock-minimo`);
  }

  /** HU-11 escenarios 1 y 6: guarda el nuevo umbral de un medicamento. */
  actualizar(idMedicamento: number, datos: ActualizarStockMinimoRequest): Observable<ActualizarStockMinimoResponse> {
    return this.http.put<ActualizarStockMinimoResponse>(`${this.apiUrl}/${idMedicamento}/stock-minimo`, datos);
  }
}
