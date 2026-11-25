import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const CONTRASENIA_ADMIN = process.env.CONTRASENIA ?? 'Prueba123!';
const MYSQL = process.env.MYSQL ?? 'mysql -h127.0.0.1 -uroot -proot -N etbotfarma';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

const SUFIJO = Date.now().toString(36);
const NOMBRE = `E2E Busqueda ${SUFIJO}`;
const LOTE_ROJO = `E2E-ROJO-${SUFIJO}`;
const LOTE_VERDE = `E2E-VERDE-${SUFIJO}`;

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

async function aparece(pagina, texto, tiempo = 5000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

const idMedicamento = consultar(
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE}', 'Sustancia de prueba', 'Tabletas', '500 mg', 0, 100, 1, NOW()); SELECT LAST_INSERT_ID();`,
);
consultar(
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, '${LOTE_VERDE}', DATE_ADD(CURDATE(), INTERVAL 24 MONTH), 20, CURDATE()), (${idMedicamento}, '${LOTE_ROJO}', DATE_ADD(CURDATE(), INTERVAL 2 MONTH), 5, CURDATE())`,
);

const navegador = await chromium.launch();
const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
const pagina = await contexto.newPage();

try {
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', 'admin');
  await pagina.fill('#contrasenia', CONTRASENIA_ADMIN);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });

  await pagina
    .getByRole('navigation', { name: 'Menú principal' })
    .getByRole('link', { name: /Inventario/ })
    .click();
  await pagina.waitForURL('**/inventario');
  revisar('El menú lleva a "Inventario de medicamentos"', await aparece(pagina, 'Inventario de medicamentos'));

  const buscador = pagina.getByLabel('Buscar medicamento');
  await buscador.fill(NOMBRE.toUpperCase());
  const filas = pagina.locator('tbody tr');
  await filas.first().waitFor({ timeout: 5000 });
  revisar('Buscar sin distinguir mayúsculas encuentra los 2 lotes', (await filas.count()) === 2);
  revisar('El lote que caduca primero va en el primer renglón', (await filas.nth(0).innerText()).includes(LOTE_ROJO));
  revisar('El renglón del lote próximo muestra semáforo Rojo', (await filas.nth(0).innerText()).includes('Rojo'));
  revisar('El renglón del lote lejano muestra semáforo Verde', (await filas.nth(1).innerText()).includes('Verde'));
  revisar('Con 25 unidades y mínimo 100 marca "Stock bajo"', (await filas.nth(0).innerText()).includes('Stock bajo'));
  await pagina.screenshot({ path: `${CARPETA}/hu08-1-busqueda.png` });

  await buscador.fill(LOTE_VERDE.toLowerCase());
  await pagina.waitForTimeout(300);
  revisar('Buscar por número de lote deja solo ese lote', (await filas.count()) === 1 && (await filas.first().innerText()).includes(LOTE_VERDE));

  await buscador.fill(NOMBRE);
  await pagina.getByRole('button', { name: /Próximos a caducar/ }).click();
  await pagina.waitForTimeout(300);
  revisar('El filtro Rojo se combina con el texto', (await filas.count()) === 1 && (await filas.first().innerText()).includes(LOTE_ROJO));
  await pagina.screenshot({ path: `${CARPETA}/hu08-2-filtro-rojo.png` });

  await pagina.getByRole('button', { name: /Vigentes/ }).click();
  await pagina.waitForTimeout(300);
  revisar('El filtro Verde muestra solo el lote vigente', (await filas.count()) === 1 && (await filas.first().innerText()).includes(LOTE_VERDE));

  await pagina.getByRole('button', { name: 'Todos', exact: true }).click();
  await buscador.fill(`NoExiste${SUFIJO}`);
  revisar('Sin coincidencias muestra el mensaje y no un error', await aparece(pagina, 'No se encontraron coincidencias'));
  await pagina.screenshot({ path: `${CARPETA}/hu08-3-sin-resultados.png` });

  await pagina.getByRole('button', { name: 'Limpiar' }).click();
  await filas.first().waitFor({ timeout: 5000 });
  revisar('Limpiar vuelve a mostrar el inventario completo', (await filas.count()) >= 2);
} catch (error) {
  console.log(`FALLA  La prueba se detuvo con un error: ${error.message}`);
  await pagina.screenshot({ path: `${CARPETA}/hu08-error.png` }).catch(() => {});
  fallas++;
} finally {
  await navegador.close();
}

console.log(fallas === 0 ? '\nHU-08 en navegador: todo OK' : `\nHU-08 en navegador: ${fallas} falla(s)`);
process.exit(fallas === 0 ? 0 : 1);
