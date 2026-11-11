import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, map, of, startWith, switchMap, tap } from 'rxjs';

import { UsuarioRegistrado } from '../../core/models/usuario.model';
import { UsuariosService } from '../../core/services/usuarios.service';
import { AlertaComponent } from '../../shared/components/alerta/alerta.component';

/**
 * HU-04: consulta de usuarios registrados (solo el Administrador: rolGuard en app.routes.ts).
 *   Escenario 1:      tabla con nombre completo, usuario, correo, teléfono y rol.
 *   Escenarios 4, 10: búsqueda por nombre de usuario (parcial, sin mayúsculas ni espacios: lo resuelve la API).
 *   Escenario 5:      "No se encontraron usuarios".
 *   Escenario 6:      al borrar el texto vuelve la lista completa.
 *   Escenario 11:     indicador de carga mientras llegan los datos.
 *   Escenario 12:     alerta de error con "Intentar de nuevo" y SIN datos incompletos.
 *   Escenario 13:     con la sesión expirada, la API responde 401 y sesionExpiradaInterceptor manda al login.
 *   Escenario 14:     solo lectura: no hay botones para editar, desactivar ni eliminar.
 */
@Component({
  selector: 'app-consulta-usuarios',
  imports: [ReactiveFormsModule, RouterLink, AlertaComponent],
  templateUrl: './consulta-usuarios.component.html',
  styleUrl: './consulta-usuarios.component.scss',
})
export class ConsultaUsuariosComponent {
  private readonly usuariosService = inject(UsuariosService);

  /** Texto del buscador. */
  readonly busqueda = new FormControl('', { nonNullable: true });

  readonly usuarios = signal<UsuarioRegistrado[]>([]);
  readonly cargando = signal(true);
  readonly error = signal(false);

  /** Botón "Intentar de nuevo" (escenario 12): vuelve a pedir la lista con el mismo texto. */
  private readonly reintentar = new Subject<void>();

  constructor() {
    const textoBuscado = this.busqueda.valueChanges.pipe(
      startWith(this.busqueda.value),
      map((texto) => texto.trim()),
      debounceTime(300),            // espera a que la persona deje de escribir
      distinctUntilChanged(),       // " lopez" y "lopez" son la misma búsqueda
    );

    // Cada búsqueda nueva (o un reintento) pide la lista a la API.
    // switchMap cancela la petición anterior si se escribe otra cosa antes de que responda.
    textoBuscado
      .pipe(
        switchMap((texto) => this.reintentar.pipe(startWith(undefined), map(() => texto))),
        tap(() => {
          this.cargando.set(true);
          this.error.set(false);
        }),
        switchMap((texto) =>
          this.usuariosService.consultarUsuarios(texto).pipe(
            catchError(() => {
              this.error.set(true);   // escenario 12
              return of<UsuarioRegistrado[]>([]);   // nunca se muestran datos a medias
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((lista) => {
        this.usuarios.set(lista);
        this.cargando.set(false);
      });
  }

  intentarDeNuevo(): void {
    this.reintentar.next();
  }

  limpiarBusqueda(): void {
    this.busqueda.setValue('');   // escenario 6
  }
}
