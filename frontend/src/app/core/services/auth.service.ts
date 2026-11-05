import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { IniciarSesionRequest, SesionUsuario } from '../models/sesion-usuario.model';

/**
 * HU-01: comunicación con la API de autenticación.
 * Guarda en memoria (signal) los datos del usuario con sesión activa.
 * El token NO se guarda aquí: viaja en una cookie HttpOnly que JavaScript no puede leer.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/auth`;

  /** Usuario con sesión activa (null si no hay sesión). */
  readonly usuario = signal<SesionUsuario | null>(null);

  iniciarSesion(datos: IniciarSesionRequest): Observable<SesionUsuario> {
    return this.http
      .post<SesionUsuario>(`${this.url}/iniciar-sesion`, datos)
      .pipe(tap((usuario) => this.usuario.set(usuario)));
  }

  /** Pregunta a la API si la cookie sigue siendo válida. Devuelve true o false. */
  verificarSesion(): Observable<boolean> {
    return this.http.get<SesionUsuario>(`${this.url}/sesion`).pipe(
      tap((usuario) => this.usuario.set(usuario)),
      map(() => true),
      catchError(() => {
        this.usuario.set(null);
        return of(false);
      }),
    );
  }

  limpiarSesion(): void {
    this.usuario.set(null);
  }
}
