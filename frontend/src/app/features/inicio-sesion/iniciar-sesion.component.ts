import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { SesionService } from '../../core/services/sesion.service';
import { AlertaComponent } from '../../shared/components/alerta/alerta.component';

/**
 * HU-01: pantalla de inicio de sesión.
 *   Criterio 1: usuario y contraseña obligatorios.
 *   Criterio 3: con datos correctos redirige al módulo inicial.
 *   Criterio 4: con datos incorrectos muestra un mensaje genérico.
 *   Alerta de sesión expirada cuando se llega aquí con ?expirada=1.
 *   Aviso de sesión cerrada (HU-02) cuando se llega con ?cerrada=1.
 * Diseño: Stitch "Inicio de sesión – login limpio"
 * (sin selector de rol ni bloqueo por intentos, que no piden las historias).
 */
@Component({
  selector: 'app-iniciar-sesion',
  imports: [ReactiveFormsModule, AlertaComponent],
  templateUrl: './iniciar-sesion.component.html',
  styleUrl: './iniciar-sesion.component.scss',
})
export class IniciarSesionComponent {
  private readonly fb = inject(FormBuilder);
  private readonly sesion = inject(SesionService);
  private readonly router = inject(Router);

  readonly formulario = this.fb.nonNullable.group({
    nombreUsuario: ['', Validators.required],
    contrasenia: ['', Validators.required],
  });

  private readonly parametros = inject(ActivatedRoute).snapshot.queryParamMap;
  readonly sesionExpirada = this.parametros.has('expirada');
  readonly sesionCerrada = this.parametros.has('cerrada');
  readonly mostrarContrasenia = signal(false);
  readonly enviando = signal(false);
  readonly mensajeError = signal<string | null>(null);

  iniciarSesion(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      // Accesibilidad: lleva el cursor al primer campo con error
      const primero = this.formulario.controls.nombreUsuario.invalid ? 'nombreUsuario' : 'contrasenia';
      document.getElementById(primero)?.focus();
      return;
    }

    this.enviando.set(true);
    this.mensajeError.set(null);

    this.sesion.iniciarSesion(this.formulario.getRawValue()).subscribe({
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
