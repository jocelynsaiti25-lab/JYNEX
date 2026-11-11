// =====================================================================
// Prueba en navegador de HU-02: cierre seguro de sesión (GitHub Actions con Playwright).
// Requiere: API en :5080, Angular en :4200 y MySQL con la base etbotfarma (la de PRUEBAS de GitHub).
// Solo usa datos inventados y guarda capturas en e2e/capturas/.
// =====================================================================
import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const API = process.env.API ?? 'http://localhost:5080';
const CONTRASENIA_ADMIN = process.env.CONTRASENIA ?? 'Prueba123!';
const MYSQL = process.env.MYSQL ?? 'mysql -h127.0.0.1 -uroot -proot -N etbotfarma';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

const SUFIJO = Date.now().toString(36);
const EMPLEADO = {
  nombre: 'Prueba',
  apellidos: 'Cierre E2E',
  nombreUsuario: `e2e.cierre.${SUFIJO}`,
  correo: `e2e.cierre.${SUFIJO}@etbotfarma.test`,
  telefono: '5500000002',
  contrasenia: 'Farmacia2026',
  idRol: 2,
};

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

/** Crea el empleado de prueba con la API (como lo haría el administrador en HU-03). */
async function crearEmpleado() {
  const login = await fetch(`${API}/api/sesiones/iniciar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario: 'admin', contrasenia: CONTRASENIA_ADMIN }),
  });
  const cookie = login.headers.getSetCookie()[0].split(';')[0];
  const respuesta = await fetch(`${API}/api/usuarios`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Cookie: cookie },
    body: JSON.stringify(EMPLEADO),
  });
  if (respuesta.status !== 201) throw new Error(`No se pudo crear el empleado de prueba: ${respuesta.status}`);
  await fetch(`${API}/api/sesiones/cerrar`, { method: 'POST', headers: { Cookie: cookie } });
}

async function aparece(pagina, texto, tiempo = 5000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

async function iniciarSesion(pagina, nombreUsuario, contrasenia) {
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', nombreUsuario);
  await pagina.fill('#contrasenia', contrasenia);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });
}

const botonCerrar = (pagina) => pagina.getByRole('button', { name: /Cerrar sesión|Cerrando sesión/ });

async function cerrarSesion(pagina) {
  await botonCerrar(pagina).click();
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 });
}

/**
 * true si la sesión MÁS RECIENTE del usuario (la que abrió esta prueba) quedó cerrada en MySQL.
 * Se revisa solo la última porque otras pruebas (HU-01, HU-03) dejan sesiones del admin abiertas.
 */
function ultimaSesionCerrada(nombreUsuario) {
  return consultar(
    `SELECT s.fecha_cierre IS NOT NULL FROM sesion s JOIN usuario u ON u.id_usuario = s.id_usuario WHERE u.nombre_usuario = '${nombreUsuario}' ORDER BY s.id_sesion DESC LIMIT 1`) === '1';
}

await crearEmpleado();
const navegador = await chromium.launch();

// ---------------------------------------------------------------------
// Escenarios 1, 2, 3, 5 y 13
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);

  // Escenario 2: visible para el Administrador en todas las pantallas
  revisar('2a. Administrador: "Cerrar sesión" visible en el inicio', await botonCerrar(pagina).isVisible());
  await pagina.goto(`${URL}/usuarios/crear`);
  await pagina.waitForSelector('#nombre');
  revisar('2b. Administrador: "Cerrar sesión" visible en Crear usuario', await botonCerrar(pagina).isVisible());
  await pagina.screenshot({ path: `${CARPETA}/hu02-1-boton-cerrar-sesion.png`, fullPage: true });
  await pagina.goto(`${URL}/inicio`);
  await pagina.waitForURL('**/inicio');

  // Escenario 1: se invalida en el servidor, va al login y muestra el mensaje exacto
  await cerrarSesion(pagina);
  revisar('1a. Al cerrar sesión llega al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  revisar('1b. Muestra "Sesión cerrada correctamente"', await aparece(pagina, 'Sesión cerrada correctamente'));
  revisar('1c. La sesión quedó cerrada en MySQL (fecha_cierre)', ultimaSesionCerrada('admin'));
  await pagina.screenshot({ path: `${CARPETA}/hu02-2-sesion-cerrada.png`, fullPage: true });

  // Escenario 3: el botón Atrás no muestra la página protegida
  await pagina.goBack();
  await pagina.waitForTimeout(1500);
  revisar('3a. Con "Atrás" sigue en el inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  revisar('3b. Con "Atrás" no se ve información de la sesión anterior', !(await pagina.getByText('Administrador del Sistema').isVisible()));

  // Escenario 5: escribir directo una URL protegida
  await pagina.goto(`${URL}/usuarios/crear`);
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 });
  revisar('5. Una URL protegida escrita a mano manda al inicio de sesión', pagina.url().includes('/iniciar-sesion'));

  // Escenario 13: otra persona en el mismo navegador
  revisar('13a. El formulario de inicio de sesión aparece vacío', (await pagina.inputValue('#nombreUsuario')) === '');
  await iniciarSesion(pagina, EMPLEADO.nombreUsuario, EMPLEADO.contrasenia);
  revisar('13b. Se ve el nombre de la nueva persona', await aparece(pagina, `${EMPLEADO.nombre} ${EMPLEADO.apellidos}`));
  revisar('13c. No se ve el nombre del usuario anterior', !(await pagina.getByText('Administrador del Sistema').isVisible()));

  // Escenario 2: también visible para el Empleado
  revisar('2c. Empleado: "Cerrar sesión" visible', await botonCerrar(pagina).isVisible());
  await cerrarSesion(pagina);
  await contexto.close();
}

// ---------------------------------------------------------------------
// Escenarios 4, 6 y 9: otra pestaña con la sesión anterior
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pestanaA = await contexto.newPage();
  await iniciarSesion(pestanaA, 'admin', CONTRASENIA_ADMIN);
  const pestanaB = await contexto.newPage();
  await pestanaB.goto(`${URL}/inicio`);
  await pestanaB.waitForURL('**/inicio');

  await cerrarSesion(pestanaB);

  // Escenario 9: la pestaña A intenta trabajar (cambiar de pantalla)
  await pestanaA.getByRole('link', { name: 'Crear usuario' }).first().click().catch(() => pestanaA.goto(`${URL}/usuarios/crear`));
  await pestanaA.waitForURL('**/iniciar-sesion**', { timeout: 15000 });
  revisar('9. La otra pestaña ya no puede trabajar y va al inicio de sesión', pestanaA.url().includes('/iniciar-sesion'));

  // Escenario 4: recargar una página protegida que quedó abierta
  await pestanaB.goto(`${URL}/inicio`);
  await pestanaB.waitForURL('**/iniciar-sesion**', { timeout: 15000 });
  await pestanaB.reload();
  await pestanaB.waitForTimeout(1000);
  revisar('4. Al recargar después de cerrar sesión sigue en el inicio de sesión', pestanaB.url().includes('/iniciar-sesion'));
  await contexto.close();
}

// ---------------------------------------------------------------------
// Escenarios 7 y 8: cambios sin guardar
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);
  await pagina.goto(`${URL}/usuarios/crear`);
  await pagina.waitForSelector('#nombre');
  await pagina.fill('#nombre', 'Sin guardar');

  // Escenario 7: cancela
  await botonCerrar(pagina).click();
  const pregunta = pagina.getByRole('dialog', { name: '¿Salir sin guardar?' });
  revisar('7a. Con datos sin guardar pide confirmación antes de cerrar', await pregunta.waitFor({ timeout: 5000 }).then(() => true, () => false));
  await pagina.screenshot({ path: `${CARPETA}/hu02-3-confirmacion-cambios.png`, fullPage: true });
  await pregunta.getByRole('button', { name: 'Seguir editando' }).click();
  await pagina.waitForTimeout(500);
  revisar('7b. Al cancelar se queda en la misma pantalla con los datos', pagina.url().includes('/usuarios/crear') && (await pagina.inputValue('#nombre')) === 'Sin guardar');
  revisar('7c. Al cancelar la sesión sigue activa', !ultimaSesionCerrada('admin') && (await botonCerrar(pagina).isEnabled()));

  // Escenario 8: confirma
  await botonCerrar(pagina).click();
  await pregunta.getByRole('button', { name: 'Salir sin guardar' }).click();
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 });
  revisar('8a. Al confirmar se cierra la sesión y va al inicio de sesión', await aparece(pagina, 'Sesión cerrada correctamente'));
  revisar('8b. Los datos sin guardar no llegaron a la base', consultar("SELECT COUNT(*) FROM usuario WHERE nombre = 'Sin guardar'") === '0');
  await contexto.close();
}

// ---------------------------------------------------------------------
// Escenario 10: la sesión ya había expirado
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, EMPLEADO.nombreUsuario, EMPLEADO.contrasenia);
  consultar(`UPDATE sesion s JOIN usuario u ON u.id_usuario = s.id_usuario SET s.fecha_inicio = UTC_TIMESTAMP() - INTERVAL 60 MINUTE, s.ultima_actividad = UTC_TIMESTAMP() - INTERVAL 30 MINUTE, s.fecha_expiracion = UTC_TIMESTAMP() - INTERVAL 10 MINUTE WHERE u.nombre_usuario = '${EMPLEADO.nombreUsuario}' AND s.fecha_cierre IS NULL`);
  await cerrarSesion(pagina);
  revisar('10a. Con la sesión expirada, cerrar sesión no muestra error', await aparece(pagina, 'Sesión cerrada correctamente'));
  revisar('10b. Sin mensajes de error en pantalla', !(await pagina.getByText('No se pudo').isVisible()));
  await contexto.close();
}

// ---------------------------------------------------------------------
// Escenarios 11 y 12: sin conexión y clics repetidos
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);

  // Escenario 12: varios clics seguidos → una sola petición
  let peticiones = 0;
  pagina.on('request', (peticion) => {
    if (peticion.url().includes('/api/sesiones/cerrar')) peticiones++;
  });
  await botonCerrar(pagina).evaluate((boton) => { boton.click(); boton.click(); boton.click(); });
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 });
  await pagina.waitForTimeout(500);
  revisar('12. Tres clics seguidos envían UNA sola petición al servidor', peticiones === 1);

  // Escenario 11: el servidor no responde
  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);
  await pagina.route('**/api/sesiones/cerrar', (ruta) => ruta.abort('internetdisconnected'));
  await cerrarSesion(pagina);
  revisar('11a. Sin conexión igual llega al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  revisar('11b. Muestra la alerta de problema de conexión', await aparece(pagina, 'Problema de conexión'));
  revisar('11c. Se limpiaron los datos de sesión del navegador', await pagina.evaluate(() => sessionStorage.getItem('etbotfarma_habia_sesion') === null));
  await pagina.screenshot({ path: `${CARPETA}/hu02-4-sin-conexion.png`, fullPage: true });

  // Al volver la conexión, el cierre pendiente se reintenta antes de mostrar el login
  await pagina.unroute('**/api/sesiones/cerrar');
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.waitForSelector('#nombreUsuario');
  await pagina.waitForTimeout(1000);
  revisar('11d. Con conexión, el cierre pendiente se completa en el servidor', ultimaSesionCerrada('admin'));
  await pagina.goto(`${URL}/inicio`);
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 });
  revisar('11e. Nadie puede entrar con la sesión anterior', pagina.url().includes('/iniciar-sesion'));
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-02 en navegador: todas las pruebas pasaron' : `HU-02 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
