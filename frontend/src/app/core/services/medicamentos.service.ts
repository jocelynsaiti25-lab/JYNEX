import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  EditarMedicamentoRequest,
  EditarMedicamentoResponse,
  MedicamentoParaEditar,
  RegistrarMedicamentoRequest,
  RegistrarMedicamentoResponse,
} from '../models/medicamento.model';

/** Servicio HTTP para operaciones del catálogo de medicamentos (HU-06 y HU-09). */
@Injectable({ providedIn: 'root' })
export class MedicamentosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/medicamentos`;

  registrar(datos: RegistrarMedicamentoRequest): Observable<RegistrarMedicamentoResponse> {
    return this.http.post<RegistrarMedicamentoResponse>(this.apiUrl, datos);
  }

  /** HU-09 escenario 2: datos para precargar el formulario de edición. */
  obtenerParaEditar(idMedicamento: number): Observable<MedicamentoParaEditar> {
    return this.http.get<MedicamentoParaEditar>(`${this.apiUrl}/${idMedicamento}`);
  }

  /** HU-09: guarda los datos descriptivos editados. */
  editar(idMedicamento: number, datos: EditarMedicamentoRequest): Observable<EditarMedicamentoResponse> {
    return this.http.put<EditarMedicamentoResponse>(`${this.apiUrl}/${idMedicamento}`, datos);
  }
}