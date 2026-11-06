import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { IniciarSesionRequest, IniciarSesionResponse } from '../models/sesion.model';

/** Marca (sin datos sensibles) para saber, aunque se recargue la página, que hubo una sesión. */
const MARCA_SESION = 'etbotfarma_habia_sesion';

/**
 * HU-01: comunicación con la API de autenticación.
 * Guarda en memoria (signal) los datos del usuario con sesión activa.
 * El token NO se guarda aquí: viaja en una cookie HttpOnly que JavaScript no puede leer.
 */
@Injectable({ providedIn: 'root' })
export class SesionService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/sesiones`;

  /** Usuario con sesión activa (null si no hay sesión). */
  readonly usuario = signal<IniciarSesionResponse | null>(null);

  iniciarSesion(datos: IniciarSesionRequest): Observable<IniciarSesionResponse> {
    return this.http
      .post<IniciarSesionResponse>(`${this.url}/iniciar`, datos)
      .pipe(tap((usuario) => this.guardarUsuario(usuario)));
  }

  /** Pregunta a la API si la cookie sigue siendo válida. Devuelve true o false. */
  verificarSesion(): Observable<boolean> {
    return this.http.get<IniciarSesionResponse>(`${this.url}/actual`).pipe(
      tap((usuario) => this.guardarUsuario(usuario)),
      map(() => true),
      catchError(() => {
        this.usuario.set(null);
        return of(false);
      }),
    );
  }

  /** true si en esta pestaña hubo una sesión (sirve para mostrar el aviso "sesión expirada"). */
  habiaSesion(): boolean {
    return this.usuario() !== null || sessionStorage.getItem(MARCA_SESION) === '1';
  }

  limpiarSesion(): void {
    this.usuario.set(null);
    sessionStorage.removeItem(MARCA_SESION);
  }

  private guardarUsuario(usuario: IniciarSesionResponse): void {
    this.usuario.set(usuario);
    sessionStorage.setItem(MARCA_SESION, '1');
  }
}
