import { HttpErrorResponse } from '@angular/common/http';
import { Component, HostListener, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Observable, Subject } from 'rxjs';

import { ConCambiosSinGuardar } from '../../core/guards/cambios-sin-guardar.guard';
import { CrearUsuarioResponse, ROLES } from '../../core/models/usuario.model';
import { UsuariosService } from '../../core/services/usuarios.service';
import { AlertaComponent } from '../../shared/components/alerta/alerta.component';
import { ConfirmacionComponent } from '../../shared/components/confirmacion/confirmacion.component';
import { mensajeContrasenia, validadoresContrasenia } from '../../shared/validators/contrasenia';

/** Respuesta de error de la API (ProblemDetails de GlobalExceptionHandler). */
interface ErrorApi {
  detail?: string;
  errores?: string[];
}

type Campo = 'nombre' | 'apellidos' | 'nombreUsuario' | 'correo' | 'telefono' | 'contrasenia' | 'idRol';

/**
 * HU-03: creación de usuarios (solo Administrador).
 *   Criterio 2: nombre, apellidos, usuario, correo, teléfono, contraseña y rol.
 *   Criterio 3: se revisan formatos aquí (ayuda) y en el servidor (validación definitiva).
 *   Criterio 4: confirmación con nombre, usuario, correo y rol ANTES de crear (sin contraseña).
 *   Criterio 6: mensaje de éxito, o del dato duplicado (correo o usuario).
 *   Escenario 16: aviso al salir con datos sin guardar.
 */
@Component({
  selector: 'app-crear-usuario',
  imports: [ReactiveFormsModule, AlertaComponent, ConfirmacionComponent],
  templateUrl: './crear-usuario.component.html',
  styleUrl: './crear-usuario.component.scss',
})
export class CrearUsuarioComponent implements ConCambiosSinGuardar {
  private readonly fb = inject(FormBuilder);
  private readonly usuarios = inject(UsuariosService);

  readonly roles = ROLES;

  readonly formulario = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.maxLength(50)]],
    apellidos: ['', [Validators.required, Validators.maxLength(80)]],
    nombreUsuario: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50), Validators.pattern(/^[a-zA-Z0-9._-]+$/)]],
    correo: ['', [Validators.required, Validators.maxLength(100), Validators.pattern(/^[^@\s]+@[^@\s]+\.[^@\s]+$/)]],
    telefono: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    // Mismas reglas que CrearUsuarioValidator (backend): 8 a 100 caracteres, mayúscula, minúscula y número
    contrasenia: ['', [Validators.required, ...validadoresContrasenia]],   // política compartida con HU-05
    idRol: [0, [Validators.min(1)]],
  });

  readonly mostrarContrasenia = signal(false);
  readonly confirmando = signal(false);
  readonly guardando = signal(false);
  readonly usuarioCreado = signal<CrearUsuarioResponse | null>(null);
  readonly errores = signal<string[]>([]);

  // ---------- Escenario 16: salir con datos sin guardar ----------
  readonly preguntandoSalida = signal(false);
  private respuestaSalida: Subject<boolean> | null = null;

  /** Nombre del rol elegido (para el resumen de la confirmación). */
  nombreRol(): string {
    return this.roles.find((rol) => rol.id === Number(this.formulario.controls.idRol.value))?.nombre ?? '';
  }

  /** 1. "Guardar": revisa el formulario y, si está bien, abre la confirmación (criterio 4). */
  revisar(): void {
    this.usuarioCreado.set(null);
    this.errores.set([]);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      // Accesibilidad: lleva el cursor al primer campo con error
      const primero = (Object.keys(this.formulario.controls) as Campo[]).find((c) => this.formulario.controls[c].invalid);
      if (primero) {
        document.getElementById(primero)?.focus();
      }
      return;
    }

    this.confirmando.set(true);
  }

  /** 2. "Crear usuario" en la confirmación: se envía a la API. */
  crear(): void {
    this.guardando.set(true);   // escenario 15: desactiva los botones mientras se procesa
    const datos = this.formulario.getRawValue();

    this.usuarios
      .crearUsuario({
        ...datos,
        nombre: datos.nombre.trim(),
        apellidos: datos.apellidos.trim(),
        nombreUsuario: datos.nombreUsuario.trim(),
        correo: datos.correo.trim(),
        idRol: Number(datos.idRol),
      })
      .subscribe({
        next: (usuario) => {
          this.guardando.set(false);
          this.confirmando.set(false);
          this.usuarioCreado.set(usuario);
          this.formulario.reset();   // queda limpio: ya no hay cambios sin guardar
        },
        error: (error: HttpErrorResponse) => {
          // Escenarios 3 y 14: no se pierde lo capturado
          this.guardando.set(false);
          this.confirmando.set(false);
          this.errores.set(this.obtenerErrores(error));
        },
      });
  }

  campoInvalido(campo: Campo): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && control.touched;
  }

  /** Mensaje de error del campo (mismos textos que el backend). */
  mensaje(campo: Campo): string {
    const errores: ValidationErrors = this.formulario.controls[campo].errors ?? {};
    const tiene = (clave: string) => clave in errores;

    if (tiene('required') || (campo === 'idRol' && tiene('min'))) {
      return MENSAJES_OBLIGATORIO[campo];
    }
    switch (campo) {
      case 'correo':
        return 'Formato de correo inválido.';
      case 'telefono':
        return 'Formato de teléfono inválido: escribe 10 dígitos, sin espacios ni guiones.';
      case 'nombreUsuario':
        return 'Entre 3 y 50 caracteres: letras, números, punto, guion y guion bajo (sin espacios).';
      case 'contrasenia':
        return mensajeContrasenia(errores);
      default:
        return 'Es demasiado largo.';
    }
  }

  /** Lo usa cambiosSinGuardarGuard al cambiar de pantalla. */
  puedeSalir(): boolean | Observable<boolean> {
    if (!this.formulario.dirty || this.guardando()) {
      return true;
    }
    this.respuestaSalida = new Subject<boolean>();
    this.preguntandoSalida.set(true);
    return this.respuestaSalida.asObservable();
  }

  responderSalida(salir: boolean): void {
    this.preguntandoSalida.set(false);
    this.respuestaSalida?.next(salir);
    this.respuestaSalida?.complete();
    this.respuestaSalida = null;
  }

  /** Si se cierra o recarga la pestaña con datos sin guardar, el navegador pregunta. */
  @HostListener('window:beforeunload', ['$event'])
  avisarAlCerrarPestana(evento: BeforeUnloadEvent): void {
    if (this.formulario.dirty) {
      evento.preventDefault();
    }
  }

  private obtenerErrores(error: HttpErrorResponse): string[] {
    const cuerpo = (error.error ?? {}) as ErrorApi;

    if (error.status === 400 && cuerpo.errores?.length) {
      return cuerpo.errores;   // validaciones del servidor
    }
    if (error.status === 422 && cuerpo.detail) {
      return [cuerpo.detail];   // "Este correo ya está registrado.", "El nombre de usuario ya está en uso..."
    }
    if (error.status === 403) {
      return ['No tienes permiso para crear usuarios.'];
    }
    return ['No se pudo crear el usuario. Revisa tu conexión e intenta de nuevo.'];
  }
}

const MENSAJES_OBLIGATORIO: Record<Campo, string> = {
  nombre: 'El nombre es obligatorio.',
  apellidos: 'Los apellidos son obligatorios.',
  nombreUsuario: 'El nombre de usuario es obligatorio.',
  correo: 'El correo electrónico es obligatorio.',
  telefono: 'El teléfono es obligatorio.',
  contrasenia: 'La contraseña es obligatoria.',
  idRol: 'Selecciona un rol (Administrador o Empleado).',
};
