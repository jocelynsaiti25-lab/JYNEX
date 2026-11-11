import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { IniciarSesionRequest, IniciarSesionResponse } from '../models/sesion.model';

/** Marca (sin datos sensibles) para saber, aunque se recargue la página, que hubo una sesión. */
const MARCA_SESION = 'etbotfarma_habia_sesion';

/**
 * HU-02 escenario 11: marca de "cierre pendiente" cuando el servidor no respondió al cerrar sesión.
 * Se guarda en localStorage (sobrevive aunque se cierre la pestaña) para volver a pedir el cierre
 * en cuanto haya conexión, antes de que otra persona use el mismo navegador (escenario 13).
 */
const MARCA_CIERRE_PENDIENTE = 'etbotfarma_cierre_pendiente';

/**
 * HU-01 y HU-02: comunicación con la API de autenticación (iniciar y cerrar sesión).
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

  /**
   * HU-02: pide al servidor que cierre la sesión (invalida el token y borra la cookie HttpOnly).
   * Pase lo que pase, los datos de sesión del navegador se limpian (escenarios 10 y 11).
   * Si el servidor no respondió, se deja la marca de "cierre pendiente" para reintentarlo después.
   */
  cerrarSesion(): Observable<void> {
    return this.http.post<void>(`${this.url}/cerrar`, null).pipe(
      tap({
        next: () => localStorage.removeItem(MARCA_CIERRE_PENDIENTE),
        error: () => localStorage.setItem(MARCA_CIERRE_PENDIENTE, '1'),
      }),
      finalize(() => this.limpiarSesion()),
    );
  }

  /** true si quedó un cierre de sesión sin confirmar por el servidor (escenario 11). */
  hayCierrePendiente(): boolean {
    return localStorage.getItem(MARCA_CIERRE_PENDIENTE) === '1';
  }

  private guardarUsuario(usuario: IniciarSesionResponse): void {
    this.usuario.set(usuario);
    sessionStorage.setItem(MARCA_SESION, '1');
  }
}
