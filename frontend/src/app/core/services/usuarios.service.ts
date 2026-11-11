import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CrearUsuarioRequest, CrearUsuarioResponse, UsuarioRegistrado } from '../models/usuario.model';

/**
 * Comunicación con la API de usuarios (api/usuarios).
 * HU-03: crear usuario · HU-04: consultar y buscar usuarios. (HU-05 agregará aquí su método.)
 */
@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/usuarios`;

  crearUsuario(datos: CrearUsuarioRequest): Observable<CrearUsuarioResponse> {
    return this.http.post<CrearUsuarioResponse>(this.url, datos);
  }

  /**
   * HU-04: GET api/usuarios?busqueda=lopez
   * Sin texto (o solo espacios) no se manda el parámetro y la API devuelve todas las cuentas.
   */
  consultarUsuarios(busqueda = ''): Observable<UsuarioRegistrado[]> {
    const texto = busqueda.trim();
    const params = texto ? new HttpParams().set('busqueda', texto) : undefined;
    return this.http.get<UsuarioRegistrado[]>(this.url, { params });
  }
}
