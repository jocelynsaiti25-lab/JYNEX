import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { CuentaUsuario } from '../../core/models/usuario.model';
import { SesionService } from '../../core/services/sesion.service';
import { UsuariosService } from '../../core/services/usuarios.service';
import { AlertaComponent } from '../../shared/components/alerta/alerta.component';
import { ConfirmacionComponent } from '../../shared/components/confirmacion/confirmacion.component';
import { mensajeContrasenia, validadoresContrasenia } from '../../shared/validators/contrasenia';

/** Respuesta de error de la API (ProblemDetails de GlobalExceptionHandler). */
interface ErrorApi {
  detail?: string;
  errores?: string[];
}

type Campo = 'nuevaContrasenia' | 'confirmarContrasenia';

/**
 * HU-05: reasignación de contraseña (solo el Administrador: rolGuard en app.routes.ts).
 * La cuenta se elige en la consulta de usuarios (HU-04): /usuarios/{idUsuario}/contrasenia.
 *   Escenario 4:      solo "Nueva contraseña" y "Confirmar contraseña" (nunca la anterior).
 *   Escenarios 9-11:  "Este campo es obligatorio", política de contraseña y "Las contraseñas no coinciden";
 *                     si hay errores NO se abre la confirmación.
 *   Escenarios 2, 3:  confirmación con nombre, usuario y correo (sin la contraseña); "Cancelar" no cambia nada.
 *   Escenario 1:      "Contraseña actualizada correctamente".
 *   Escenarios 7, 8:  "La cuenta no existe" / "La cuenta está desactivada".
 *   Escenario 15:     alerta de error; el servidor no deja nada a medias (una sola transacción).
 *   Escenario 16:     botones desactivados mientras se procesa: el cambio se hace una sola vez.
 *   Escenario 17:     si es su propia cuenta, se cierra su sesión y va al inicio de sesión.
 */
@Component({
  selector: 'app-reasignar-contrasena',
  imports: [ReactiveFormsModule, RouterLink, AlertaComponent, ConfirmacionComponent],
  templateUrl: './reasignar-contrasena.component.html',
  styleUrl: './reasignar-contrasena.component.scss',
})
export class ReasignarContrasenaComponent implements OnInit {
  private readonly usuarios = inject(UsuariosService);
  private readonly sesion = inject(SesionService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  /** Viene de la dirección /usuarios/:idUsuario/contrasenia. */
  private readonly idUsuario = Number(inject(ActivatedRoute).snapshot.paramMap.get('idUsuario'));

  readonly cuenta = signal<CuentaUsuario | null>(null);
  readonly cargando = signal(true);
  readonly errorCarga = signal<string | null>(null);

  readonly formulario = this.fb.nonNullable.group(
    {
      nuevaContrasenia: ['', [Validators.required, ...validadoresContrasenia]],   // misma política que HU-03
      confirmarContrasenia: ['', Validators.required],
    },
    { validators: contraseniasCoinciden },
  );

  readonly mostrarContrasenias = signal(false);
  readonly confirmando = signal(false);
  readonly guardando = signal(false);
  readonly exito = signal(false);
  readonly errores = signal<string[]>([]);

  ngOnInit(): void {
    this.cargarCuenta();
  }

  cargarCuenta(): void {
    this.cargando.set(true);
    this.errorCarga.set(null);

    this.usuarios.obtenerCuenta(this.idUsuario).subscribe({
      next: (cuenta) => {
        this.cuenta.set(cuenta);
        this.cargando.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.cargando.set(false);
        // Escenario 7: un id escrito a mano que no existe (o que no es un número)
        this.errorCarga.set(error.status === 404 || error.status === 400
          ? 'La cuenta no existe'
          : 'No se pudo cargar la cuenta. Revisa tu conexión e intenta de nuevo.');
      },
    });
  }

  /** 1. "Guardar": si hay errores se marcan y NO se abre la confirmación (escenarios 9, 10, 11). */
  guardar(): void {
    this.exito.set(false);
    this.errores.set([]);

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      const primero: Campo = this.formulario.controls.nuevaContrasenia.invalid ? 'nuevaContrasenia' : 'confirmarContrasenia';
      document.getElementById(primero)?.focus();
      return;
    }

    this.confirmando.set(true);   // escenario 2
  }

  /** 2. "Aceptar" en la confirmación: se envía a la API una sola vez (escenario 16). */
  reasignar(): void {
    const cuenta = this.cuenta();
    if (!cuenta || this.guardando()) {
      return;
    }
    this.guardando.set(true);

    this.usuarios.reasignarContrasenia(cuenta.idUsuario, this.formulario.getRawValue()).subscribe({
      next: (resultado) => {
        // Escenario 17: cambió SU contraseña → su sesión se cerró; debe entrar con la nueva
        if (resultado.eraSuPropiaCuenta) {
          this.sesion.limpiarSesion();
          this.router.navigate(['/iniciar-sesion'], { queryParams: { contrasenia: 'actualizada' }, replaceUrl: true });
          return;
        }
        this.guardando.set(false);
        this.confirmando.set(false);
        this.formulario.reset();   // la contraseña no se queda escrita en pantalla
        this.exito.set(true);      // escenario 1
      },
      error: (error: HttpErrorResponse) => {
        this.guardando.set(false);
        this.confirmando.set(false);
        this.errores.set(obtenerErrores(error));
      },
    });
  }

  campoInvalido(campo: Campo): boolean {
    const control = this.formulario.controls[campo];
    const noCoinciden = campo === 'confirmarContrasenia' && this.formulario.hasError('noCoinciden');
    return (control.invalid || noCoinciden) && control.touched;
  }

  mensaje(campo: Campo): string {
    const errores: ValidationErrors = this.formulario.controls[campo].errors ?? {};
    if ('required' in errores) {
      return 'Este campo es obligatorio';   // escenario 11
    }
    if (campo === 'confirmarContrasenia') {
      return 'Las contraseñas no coinciden';   // escenario 10
    }
    return mensajeContrasenia(errores);   // escenario 9
  }
}

/** Escenario 10: "Confirmar contraseña" debe ser igual a "Nueva contraseña". */
function contraseniasCoinciden(grupo: AbstractControl): ValidationErrors | null {
  const nueva = grupo.get('nuevaContrasenia')?.value ?? '';
  const confirmar = grupo.get('confirmarContrasenia')?.value ?? '';
  return nueva && confirmar && nueva !== confirmar ? { noCoinciden: true } : null;
}

/** Mensajes de error de la API. */
function obtenerErrores(error: HttpErrorResponse): string[] {
  const cuerpo = (error.error ?? {}) as ErrorApi;

  if (error.status === 400 && cuerpo.errores?.length) {
    return cuerpo.errores;   // validaciones del servidor
  }
  if ((error.status === 404 || error.status === 422) && cuerpo.detail) {
    return [cuerpo.detail];   // "La cuenta no existe" / "La cuenta está desactivada"
  }
  if (error.status === 403) {
    return ['No tienes permiso para reasignar contraseñas.'];
  }
  // Escenario 15: el servidor no respondió o falló; no quedó ningún cambio a medias
  return ['No se pudo actualizar la contraseña. Revisa tu conexión e intenta de nuevo.'];
}
