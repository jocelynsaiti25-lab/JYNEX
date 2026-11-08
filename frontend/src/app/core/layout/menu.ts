/**
 * Menú principal de la aplicación (barra lateral y accesos del inicio).
 *
 * CÓMO AGREGAR TU PANTALLA (para cada compañera):
 *   1. Crea la ruta en app.routes.ts dentro de los "hijos" del LayoutComponent.
 *   2. Aquí, escribe esa misma ruta en `ruta` de tu módulo.
 *   Mientras `ruta` no exista, el módulo NO aparece en la barra lateral
 *   y en el inicio se muestra como "Próximamente".
 *
 * `roles`: quién puede verlo. Si no se pone, lo ven todos los usuarios con sesión.
 * Ocultar un botón NO es seguridad: la API también debe revisar el rol.
 */
export type Rol = 'Administrador' | 'Empleado';

export interface OpcionMenu {
  etiqueta: string;
  descripcion: string;
  /** Nombre del ícono de Material Symbols (https://fonts.google.com/icons). */
  icono: string;
  /** Historias de usuario que se atienden en esta pantalla (solo referencia del equipo; no se muestra). */
  historias: string[];
  ruta?: string;
  roles?: Rol[];
}

export const MENU: OpcionMenu[] = [
  {
    etiqueta: 'Inicio',
    descripcion: 'Resumen y accesos rápidos.',
    icono: 'home',
    historias: ['HU-01'],
    ruta: '/inicio',
  },
  {
    etiqueta: 'Inventario',
    descripcion: 'Buscar medicamentos, semáforo de caducidad, stock mínimo, agotados y baja de lotes.',
    icono: 'inventory_2',
    historias: ['HU-08', 'HU-09', 'HU-10', 'HU-11', 'HU-12', 'HU-14'],
  },
  {
    etiqueta: 'Registrar medicamento',
    descripcion: 'Dar de alta un medicamento nuevo con su primer lote.',
    icono: 'medication',
    historias: ['HU-06'],
  },
  {
    etiqueta: 'Entrada de lote',
    descripcion: 'Registrar un lote nuevo de un medicamento existente.',
    icono: 'move_to_inbox',
    historias: ['HU-07'],
  },
  {
    etiqueta: 'Salida de medicamentos',
    descripcion: 'Registrar salidas con propuesta automática de lotes (FEFO).',
    icono: 'outbox',
    historias: ['HU-13'],
  },
  {
    etiqueta: 'Bitácora',
    descripcion: 'Consultar movimientos y exportarlos a Excel.',
    icono: 'history',
    historias: ['HU-15', 'HU-16'],
  },
  {
    etiqueta: 'Usuarios',
    descripcion: 'Crear y consultar usuarios, y reasignar contraseñas.',
    icono: 'manage_accounts',
    historias: ['HU-03', 'HU-04', 'HU-05'],
    roles: ['Administrador'],
  },
];

/** Opciones que el rol puede ver (con o sin pantalla terminada). */
export function opcionesPara(rol: string | undefined): OpcionMenu[] {
  return MENU.filter((opcion) => !opcion.roles || opcion.roles.includes(rol as Rol));
}
