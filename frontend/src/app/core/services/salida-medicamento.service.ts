import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  MedicamentoParaSalida,
  PropuestaSalida,
  RegistrarSalidaRequest,
  SalidaRegistrada,
} from '../models/salida-medicamento.model';

@Injectable({ providedIn: 'root' })
export class SalidaMedicamentoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/salidas-medicamento`;

  buscar(termino: string): Observable<MedicamentoParaSalida[]> {
    const params = termino.trim() === '' ? new HttpParams() : new HttpParams().set('termino', termino.trim());
    return this.http.get<MedicamentoParaSalida[]>(`${this.apiUrl}/medicamentos`, { params });
  }

  proponer(idMedicamento: number, cantidad: string): Observable<PropuestaSalida> {
    const params = new HttpParams().set('idMedicamento', idMedicamento).set('cantidad', cantidad.trim());
    return this.http.get<PropuestaSalida>(`${this.apiUrl}/propuesta`, { params });
  }

  registrar(datos: RegistrarSalidaRequest): Observable<SalidaRegistrada> {
    return this.http.post<SalidaRegistrada>(this.apiUrl, datos);
  }
}
