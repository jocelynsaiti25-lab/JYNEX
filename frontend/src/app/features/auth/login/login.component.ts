import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';

/**
 * HU-01: pantalla de inicio de sesión.
 *   Criterio 1: usuario y contraseña obligatorios.
 *   Criterio 3: con datos correctos redirige al módulo inicial.
 *   Criterio 4: con datos incorrectos muestra un mensaje genérico.
 *   Alerta de sesión expirada cuando se llega aquí con ?expirada=1.
 */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly formulario = this.fb.nonNullable.group({
    nombreUsuario: ['', Validators.required],
    contrasenia: ['', Validators.required],
  });

  readonly sesionExpirada = inject(ActivatedRoute).snapshot.queryParamMap.has('expirada');
  readonly enviando = signal(false);
  readonly mensajeError = signal<string | null>(null);

  iniciarSesion(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.mensajeError.set(null);

    this.auth.iniciarSesion(this.formulario.getRawValue()).subscribe({
      next: () => this.router.navigate(['/inicio']),
      error: (error: HttpErrorResponse) => {
        this.enviando.set(false);
        this.mensajeError.set(this.obtenerMensaje(error));
      },
    });
  }

  campoInvalido(campo: 'nombreUsuario' | 'contrasenia'): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && control.touched;
  }

  private obtenerMensaje(error: HttpErrorResponse): string {
    if (error.status === 401) {
      return 'Usuario o contraseña incorrectos.';
    }
    if (error.status === 400) {
      return 'Escribe tu nombre de usuario y tu contraseña.';
    }
    return 'No se pudo conectar con el servidor. Intenta de nuevo.';
  }
}
