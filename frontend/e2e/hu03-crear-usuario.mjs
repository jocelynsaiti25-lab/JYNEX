// =====================================================================
// Prueba en navegador de HU-03: creación de usuarios (GitHub Actions con Playwright).
// Requiere: API en :5080, Angular en :4200 y MySQL con la base etbotfarma (la de PRUEBAS de GitHub).
// Solo usa datos inventados (terminación única en cada ejecución) y guarda capturas en e2e/capturas/.
// =====================================================================
import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const CONTRASENIA_ADMIN = process.env.CONTRASENIA ?? 'Prueba123!';
const MYSQL = process.env.MYSQL ?? 'mysql -h127.0.0.1 -uroot -proot -N etbotfarma';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

// Datos de prueba controlados: nadie real, distintos en cada ejecución
const SUFIJO = Date.now().toString(36);
const EMPLEADO = {
  nombre: 'Prueba',
  apellidos: 'Empleado E2E',
  nombreUsuario: `e2e.empleado.${SUFIJO}`,
  correo: `e2e.empleado.${SUFIJO}@etbotfarma.test`,
  telefono: '5500000001',
  contrasenia: 'Farmacia2026',
  rol: 'Empleado',
};

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

/** Consulta a MySQL de pruebas y devuelve el texto de la primera columna. */
function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

const navegador = await chromium.launch();

async function abrirSesion(nombreUsuario, contrasenia) {
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  // Si el navegador pregunta "¿Salir de la página?" (datos sin guardar), se acepta
  pagina.on('dialog', (dialogo) => dialogo.accept());
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', nombreUsuario);
  await pagina.fill('#contrasenia', contrasenia);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });
  return pagina;
}

async function aparece(pagina, texto, tiempo = 5000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

async function llenar(pagina, datos) {
  await pagina.fill('#nombre', datos.nombre);
  await pagina.fill('#apellidos', datos.apellidos);
  await pagina.fill('#nombreUsuario', datos.nombreUsuario);
  await pagina.fill('#correo', datos.correo);
  await pagina.fill('#telefono', datos.telefono);
  await pagina.fill('#contrasenia', datos.contrasenia);
  await pagina.selectOption('#idRol', { label: datos.rol });
}

/** Guardar → confirmación → "Crear usuario". Devuelve el texto que mostró la confirmación. */
async function crear(pagina, datos) {
  await llenar(pagina, datos);
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  const dialogo = pagina.getByRole('dialog', { name: '¿Crear este usuario?' });
  await dialogo.waitFor({ timeout: 5000 });
  const resumen = await dialogo.innerText();
  await dialogo.getByRole('button', { name: 'Crear usuario' }).click();
  await dialogo.waitFor({ state: 'hidden', timeout: 10000 });
  return resumen;
}

// ---------------------------------------------------------------------
// 1. El administrador entra al formulario (menú "Usuarios" → lista de HU-04 → "Crear usuario")
// ---------------------------------------------------------------------
const admin = await abrirSesion('admin', CONTRASENIA_ADMIN);
await admin.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Usuarios', exact: true }).click();
await admin.waitForURL('**/usuarios');
await admin.getByRole('link', { name: 'Crear usuario' }).click();
await admin.waitForURL('**/usuarios/crear');
revisar('1. El administrador abre "Crear usuario" desde el menú', await aparece(admin, 'Datos de la persona'));
await admin.screenshot({ path: `${CARPETA}/hu03-1-formulario.png`, fullPage: true });

// ---------------------------------------------------------------------
// 2. Validaciones del formulario (no se abre la confirmación)
// ---------------------------------------------------------------------
await admin.getByRole('button', { name: 'Guardar', exact: true }).click();
revisar('2a. Campos vacíos: "El nombre es obligatorio."', await aparece(admin, 'El nombre es obligatorio.'));
revisar('2a. Campos vacíos: "El correo electrónico es obligatorio."', await aparece(admin, 'El correo electrónico es obligatorio.'));
revisar('2a. Campos vacíos: "Selecciona un rol (Administrador o Empleado)."', await aparece(admin, 'Selecciona un rol (Administrador o Empleado).'));

await llenar(admin, { ...EMPLEADO, correo: 'maria.gmail', telefono: '55-12', contrasenia: 'Corta1' });
await admin.getByRole('button', { name: 'Guardar', exact: true }).click();
revisar('2b. Correo sin @: "Formato de correo inválido."', await aparece(admin, 'Formato de correo inválido.'));
revisar('2b. Teléfono con guiones: "Formato de teléfono inválido"', await aparece(admin, 'Formato de teléfono inválido'));
revisar('2b. Contraseña corta: "al menos 8 caracteres"', await aparece(admin, 'La contraseña debe tener al menos 8 caracteres.'));

await admin.fill('#contrasenia', 'farmacia2026');
revisar('2c. Contraseña sin mayúscula: "al menos una letra mayúscula"', await aparece(admin, 'La contraseña debe tener al menos una letra mayúscula.'));
await admin.fill('#contrasenia', 'FarmaciaEtbot');
revisar('2d. Contraseña sin número: "al menos un número"', await aparece(admin, 'La contraseña debe tener al menos un número.'));
const maximo = await admin.getAttribute('#contrasenia', 'maxlength');
revisar('2e. La contraseña no acepta más de 100 caracteres (igual que el backend)', maximo === '100');

const dialogoAbierto = await admin.getByRole('dialog', { name: '¿Crear este usuario?' }).isVisible();
revisar('2f. Con errores NO se abre la confirmación', !dialogoAbierto);
await admin.screenshot({ path: `${CARPETA}/hu03-2-validaciones.png`, fullPage: true });

// ---------------------------------------------------------------------
// 3. Crear un usuario válido: confirmación (sin contraseña) y mensaje de éxito
// ---------------------------------------------------------------------
// (llenar() reemplaza todos los datos incorrectos del paso anterior)
const resumen = await crear(admin, EMPLEADO);
revisar('3a. La confirmación muestra el correo y el rol', resumen.includes(EMPLEADO.correo) && resumen.includes('Empleado'));
revisar('3b. La confirmación NO muestra la contraseña', !resumen.includes(EMPLEADO.contrasenia));
revisar('3c. Muestra "Usuario creado correctamente"', await aparece(admin, 'Usuario creado correctamente', 10000));
await admin.screenshot({ path: `${CARPETA}/hu03-3-usuario-creado.png`, fullPage: true });

// ---------------------------------------------------------------------
// 4. Mismo correo (otra persona, otro usuario): no se permite
// ---------------------------------------------------------------------
await crear(admin, {
  ...EMPLEADO,
  nombre: 'Otra',
  apellidos: 'Persona E2E',
  nombreUsuario: `e2e.otra.${SUFIJO}`,
  correo: EMPLEADO.correo.toUpperCase(),   // mayúsculas: sigue siendo el mismo correo
});
revisar('4. Correo repetido: "Este correo ya está registrado."', await aparece(admin, 'Este correo ya está registrado.', 10000));
const conEseCorreo = consultar(`SELECT COUNT(*) FROM usuario WHERE correo = '${EMPLEADO.correo}'`);
revisar('4b. En MySQL sigue habiendo UNA sola cuenta con ese correo', conEseCorreo === '1');
await admin.screenshot({ path: `${CARPETA}/hu03-4-correo-repetido.png`, fullPage: true });

// ---------------------------------------------------------------------
// 5. Mismo nombre completo y mismo teléfono, pero otro correo y otro usuario: SÍ se permite
// ---------------------------------------------------------------------
await crear(admin, {
  ...EMPLEADO,
  nombreUsuario: `e2e.empleado2.${SUFIJO}`,
  correo: `e2e.homonimo.${SUFIJO}@etbotfarma.test`,
});
revisar('5. Mismo nombre y teléfono con otro correo: se crea', await aparece(admin, 'Usuario creado correctamente', 10000));
const homonimos = consultar(
  `SELECT COUNT(*) FROM usuario WHERE nombre = 'Prueba' AND apellidos = 'Empleado E2E' AND telefono = '5500000001' AND correo LIKE '%${SUFIJO}%'`,
);
revisar('5b. En MySQL hay 2 cuentas con el mismo nombre y teléfono', homonimos === '2');

// ---------------------------------------------------------------------
// 6. Auditoría: cada usuario creado tiene su registro (quién = admin) y sin contraseña
// ---------------------------------------------------------------------
const auditoria = consultar(
  `SELECT CONCAT(quien.nombre_usuario, '|', a.accion, '|', a.valor_nuevo) FROM auditoria a ` +
    `INNER JOIN usuario creado ON creado.id_usuario = a.id_registro AND a.tabla_afectada = 'usuario' ` +
    `INNER JOIN usuario quien ON quien.id_usuario = a.id_usuario WHERE creado.nombre_usuario = '${EMPLEADO.nombreUsuario}'`,
);
revisar('6a. Auditoría: la hizo "admin" con la acción CREAR USUARIO', auditoria.startsWith('admin|CREAR USUARIO|'));
revisar('6b. Auditoría: guarda el correo del usuario creado', auditoria.includes(EMPLEADO.correo));
revisar('6c. Auditoría: NO guarda la contraseña', auditoria !== '' && !auditoria.includes(EMPLEADO.contrasenia));
const auditoriasCorreoRepetido = consultar(
  `SELECT COUNT(*) FROM auditoria a INNER JOIN usuario u ON u.id_usuario = a.id_registro WHERE u.nombre_usuario = 'e2e.otra.${SUFIJO}'`,
);
revisar('6d. El intento con correo repetido NO dejó auditoría', auditoriasCorreoRepetido === '0');

// ---------------------------------------------------------------------
// 7. Salir con datos sin guardar: pregunta antes (escenario 16)
// ---------------------------------------------------------------------
await admin.fill('#nombre', 'Sin guardar');
await admin.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Inicio', exact: true }).click();
const salir = admin.getByRole('dialog', { name: '¿Salir sin guardar?' });
const pregunto = await salir.waitFor({ timeout: 5000 }).then(() => true).catch(() => false);
revisar('7a. Al salir con datos sin guardar pregunta "¿Salir sin guardar?"', pregunto);
await salir.getByRole('button', { name: 'Seguir editando' }).click();
revisar('7b. "Seguir editando" se queda en el formulario con los datos', admin.url().includes('/usuarios/crear') && (await admin.inputValue('#nombre')) === 'Sin guardar');

// ---------------------------------------------------------------------
// 8. Un Empleado NO puede crear usuarios: no ve la opción y la URL directa lo regresa al inicio
// ---------------------------------------------------------------------
const empleado = await abrirSesion(EMPLEADO.nombreUsuario, EMPLEADO.contrasenia);
const opcionUsuarios = await empleado
  .getByRole('navigation', { name: 'Menú principal' })
  .getByRole('link', { name: 'Usuarios', exact: true })
  .count();
revisar('8a. El Empleado no ve "Usuarios" en el menú', opcionUsuarios === 0);
await empleado.goto(`${URL}/usuarios/crear`);
await empleado.waitForURL('**/inicio', { timeout: 10000 }).catch(() => {});
revisar('8b. El Empleado que escribe /usuarios/crear regresa a /inicio', empleado.url().includes('/inicio'));
const formularioVisible = await empleado.getByText('Datos de la persona').isVisible().catch(() => false);
revisar('8c. El Empleado no ve el formulario', !formularioVisible);
await empleado.screenshot({ path: `${CARPETA}/hu03-8-empleado-sin-acceso.png`, fullPage: true });

await navegador.close();

console.log('-----');
if (fallas > 0) {
  console.log(`${fallas} prueba(s) fallaron`);
  process.exit(1);
}
console.log('HU-03 en navegador: todas las pruebas pasaron');
