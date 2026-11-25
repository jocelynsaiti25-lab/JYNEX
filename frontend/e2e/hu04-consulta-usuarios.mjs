// =====================================================================
// Prueba en navegador de HU-04: consulta de usuarios registrados (GitHub Actions con Playwright).
// Requiere: API en :5080, Angular en :4200 y MySQL con la base etbotfarma (la de PRUEBAS de GitHub).
// Solo usa datos inventados (terminación única en cada ejecución) y guarda capturas en e2e/capturas/.
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
const LOPEZ = `mflopez${SUFIJO}`;        // escenarios 4, 8 y 10
const GEMELA = `mfernandez${SUFIJO}`;    // escenario 9: mismo nombre completo que LOPEZ
const EMPLEADO = `e2e.consulta.${SUFIJO}`;

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

/** Crea cuentas con la API de HU-03, como lo haría el administrador. */
async function crearCuentas() {
  const login = await fetch(`${API}/api/sesiones/iniciar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario: 'admin', contrasenia: CONTRASENIA_ADMIN }),
  });
  const cookie = login.headers.getSetCookie()[0].split(';')[0];
  const crear = async (nombreUsuario, nombre, apellidos) => {
    const respuesta = await fetch(`${API}/api/usuarios`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Cookie: cookie },
      body: JSON.stringify({
        nombre, apellidos, nombreUsuario, correo: `${nombreUsuario}@etbotfarma.test`,
        telefono: '5500000004', contrasenia: 'Farmacia#2026', idRol: 2,
      }),
    });
    if (respuesta.status !== 201) throw new Error(`No se pudo crear ${nombreUsuario}: ${respuesta.status}`);
  };
  await crear(LOPEZ, 'María', 'Fernández López');
  await crear(GEMELA, 'María', 'Fernández López');
  await crear(EMPLEADO, 'Prueba', 'Consulta E2E');
  await fetch(`${API}/api/sesiones/cerrar`, { method: 'POST', headers: { Cookie: cookie } });
}

async function iniciarSesion(pagina, nombreUsuario, contrasenia) {
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', nombreUsuario);
  await pagina.fill('#contrasenia', contrasenia);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });
}

async function aparece(pagina, texto, tiempo = 5000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

/** Nombres de usuario que se ven en la tabla (columna 2). */
async function usuariosEnTabla(pagina) {
  await pagina.waitForTimeout(600);   // espera el retraso del buscador (300 ms) y la respuesta
  if (!(await pagina.locator('table.tabla').isVisible())) return [];
  return pagina.locator('table.tabla tbody tr td:nth-child(2)').allInnerTexts();
}

async function buscar(pagina, texto) {
  await pagina.fill('#busqueda', texto);
  return usuariosEnTabla(pagina);
}

await crearCuentas();
const usuariosAntes = consultar('SELECT COUNT(*) FROM usuario');
const navegador = await chromium.launch();

// ---------------------------------------------------------------------
// Administrador: escenarios 1, 4 a 10, 14
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();

  // Escenario 7: lo que responde la API no trae contraseñas ni hashes
  const respuestasApi = [];
  pagina.on('response', async (respuesta) => {
    if (respuesta.url().includes('/api/usuarios') && respuesta.request().method() === 'GET') {
      respuestasApi.push(await respuesta.text().catch(() => ''));
    }
  });

  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);

  // Escenario 1: "Usuarios" desde el menú
  await pagina.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Usuarios', exact: true }).click();
  await pagina.waitForURL('**/usuarios');
  const todos = await usuariosEnTabla(pagina);
  const encabezados = await pagina.locator('table.tabla thead th').allInnerTexts();
  revisar('1a. La opción "Usuarios" del menú abre la lista', todos.length >= 4);
  revisar('1b. Columnas: nombre completo, usuario, correo, teléfono y rol',
    ['NOMBRE COMPLETO', 'NOMBRE DE USUARIO', 'CORREO ELECTRÓNICO', 'TELÉFONO', 'ROL'].every((c) => encabezados.map((e) => e.toUpperCase()).includes(c)));
  const filaLopez = (await pagina.locator('table.tabla tbody tr', { hasText: LOPEZ }).innerText()).replace(/\s+/g, ' ');
  revisar('1c. Cada fila muestra sus datos y su rol', filaLopez.includes('María Fernández López') && filaLopez.includes(`${LOPEZ}@etbotfarma.test`) && filaLopez.includes('5500000004') && filaLopez.includes('Empleado'));
  await pagina.screenshot({ path: `${CARPETA}/hu04-1-lista.png`, fullPage: true });

  // Escenario 8: las cuentas recién creadas (HU-03) aparecen
  revisar('8. Las cuentas recién creadas aparecen en la lista', todos.includes(LOPEZ) && todos.includes(EMPLEADO));

  // Escenario 9: mismo nombre completo, cuentas distintas
  const marias = await pagina.locator('table.tabla tbody tr', { hasText: 'María Fernández López' }).allInnerTexts();
  revisar('9. Dos personas con el mismo nombre aparecen como cuentas distintas',
    marias.filter((f) => f.includes(LOPEZ) || f.includes(GEMELA)).length === 2);

  // Escenario 4: búsqueda parcial
  const parcial = await buscar(pagina, `lopez${SUFIJO}`);
  revisar('4. La búsqueda parcial muestra solo las cuentas que coinciden', parcial.length === 1 && parcial[0] === LOPEZ);
  await pagina.screenshot({ path: `${CARPETA}/hu04-2-busqueda.png`, fullPage: true });

  // Escenario 10: espacios y mayúsculas
  const conEspacios = await buscar(pagina, `  ${LOPEZ.toUpperCase()}  `);
  revisar('10. La búsqueda ignora espacios sobrantes y mayúsculas', conEspacios.length === 1 && conEspacios[0] === LOPEZ);

  // Escenario 5: sin coincidencias
  await buscar(pagina, `noexiste${SUFIJO}`);
  revisar('5. Sin coincidencias muestra "No se encontraron usuarios"', await aparece(pagina, 'No se encontraron usuarios'));
  await pagina.screenshot({ path: `${CARPETA}/hu04-3-sin-resultados.png`, fullPage: true });

  // Escenario 6: limpiar la búsqueda
  const limpia = await buscar(pagina, '');
  revisar('6. Al borrar el texto vuelve la lista completa', limpia.length === todos.length);

  // Escenario 7: ni en pantalla ni en la respuesta de la API
  const textoPantalla = await pagina.locator('main').innerText();
  revisar('7a. La pantalla no muestra contraseñas ni hashes', !textoPantalla.includes('Farmacia#2026') && !textoPantalla.includes('AQAAAA'));
  revisar('7b. La respuesta de la API no trae contraseñas ni hashes',
    respuestasApi.length > 0 && respuestasApi.every((r) => !/contrasenia|hash"|AQAAAA|Farmacia#2026/i.test(r)));

  // Escenario 14: solo lectura
  const acciones = await pagina.getByRole('button', { name: /editar|desactivar|eliminar|borrar/i }).count();
  revisar('14a. No hay opciones para editar, desactivar ni eliminar', acciones === 0);
  revisar('14b. Consultar y buscar no modificó ninguna cuenta', consultar('SELECT COUNT(*) FROM usuario') === usuariosAntes);

  // Escenario 11: indicador de carga (la API tarda 1.5 s)
  await pagina.route('**/api/usuarios**', async (ruta) => {
    await new Promise((listo) => setTimeout(listo, 1500));
    await ruta.continue();
  });
  await pagina.reload();
  revisar('11a. Mientras llegan los datos se ve "Cargando usuarios…"', await aparece(pagina, 'Cargando usuarios…', 3000));
  revisar('11b. Después aparece la tabla', await pagina.locator('table.tabla').waitFor({ timeout: 10000 }).then(() => true, () => false));
  await pagina.unroute('**/api/usuarios**');

  // Escenario 12: error al cargar y "Intentar de nuevo"
  await pagina.route('**/api/usuarios**', (ruta) => ruta.abort('internetdisconnected'));
  await pagina.reload();
  revisar('12a. Si falla, avisa "No se pudo cargar la información"', await aparece(pagina, 'No se pudo cargar la información', 8000));
  revisar('12b. No se muestran datos incompletos', !(await pagina.locator('table.tabla').isVisible()));
  await pagina.screenshot({ path: `${CARPETA}/hu04-4-error.png`, fullPage: true });
  await pagina.unroute('**/api/usuarios**');
  await pagina.getByRole('button', { name: 'Intentar de nuevo' }).click();
  revisar('12c. "Intentar de nuevo" vuelve a cargar la lista', await pagina.locator('table.tabla').waitFor({ timeout: 10000 }).then(() => true, () => false));

  // Escenario 13: la sesión expira y luego busca
  consultar(`UPDATE sesion s JOIN usuario u ON u.id_usuario = s.id_usuario SET s.fecha_inicio = UTC_TIMESTAMP() - INTERVAL 60 MINUTE, s.ultima_actividad = UTC_TIMESTAMP() - INTERVAL 30 MINUTE, s.fecha_expiracion = UTC_TIMESTAMP() - INTERVAL 10 MINUTE WHERE u.nombre_usuario = 'admin' AND s.fecha_cierre IS NULL`);
  await pagina.fill('#busqueda', 'admin');
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 }).catch(() => {});
  revisar('13a. Con la sesión expirada, buscar manda al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  revisar('13b. Muestra el aviso de sesión expirada', await aparece(pagina, 'Tu sesión expiró por inactividad'));
  await contexto.close();
}

// ---------------------------------------------------------------------
// Empleado: escenario 2
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, EMPLEADO, 'Farmacia#2026');

  const opcion = await pagina.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Usuarios', exact: true }).count();
  revisar('2a. El Empleado no ve "Usuarios" en el menú', opcion === 0);
  await pagina.goto(`${URL}/usuarios`);
  await pagina.waitForURL('**/inicio', { timeout: 10000 }).catch(() => {});
  revisar('2b. El Empleado que escribe /usuarios regresa a su inicio', pagina.url().includes('/inicio'));
  revisar('2c. El Empleado no ve la lista', !(await pagina.locator('table.tabla').isVisible()));
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-04 en navegador: todas las pruebas pasaron' : `HU-04 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
