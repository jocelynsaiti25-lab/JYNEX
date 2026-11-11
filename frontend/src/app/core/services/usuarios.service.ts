import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CrearUsuarioRequest, CrearUsuarioResponse } from '../models/usuario.model';

/**
 * Comunicación con la API de usuarios (api/usuarios).
 * HU-03: crear usuario. (HU-04 y HU-05 agregarán aquí sus métodos.)
 */
@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/usuarios`;

  crearUsuario(datos: CrearUsuarioRequest): Observable<CrearUsuarioResponse> {
    return this.http.post<CrearUsuarioResponse>(this.url, datos);
  }
}
