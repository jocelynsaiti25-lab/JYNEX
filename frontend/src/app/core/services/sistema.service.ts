import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { EstadoSistema } from '../models/estado-sistema.model';

/** Comunicación con los endpoints generales de la API. */
@Injectable({ providedIn: 'root' })
export class SistemaService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/sistema`;

  obtenerEstado(): Observable<EstadoSistema> {
    return this.http.get<EstadoSistema>(`${this.url}/estado`);
  }
}
