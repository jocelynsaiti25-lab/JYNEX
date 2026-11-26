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
const BASE = `E2E Agotado ${SUFIJO}`;
const SIN_LOTES_EN_CERO = `${BASE} Ceros`;
const SOLO_CADUCADO = `${BASE} Caducado`;
const CON_EXISTENCIAS = `${BASE} Vigente`;
const MISMO_NOMBRE = `${BASE} Paracetamol`;
const EMPLEADO = `e2e.agot.${SUFIJO}`;

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

function crearMedicamento(nombre, presentacion, concentracion) {
  return consultar(
    `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${nombre}', 'Sustancia E2E', '${presentacion}', '${concentracion}', 0, 10, 1, NOW()); SELECT LAST_INSERT_ID();`,
  );
}

function crearLote(idMedicamento, numero, intervaloCaducidad, stock) {
  consultar(
    `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, '${numero}', ${intervaloCaducidad}, ${stock}, DATE_SUB(CURDATE(), INTERVAL 6 MONTH));`,
  );
}

async function aparece(pagina, texto, tiempo = 5000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

const idCeros = crearMedicamento(SIN_LOTES_EN_CERO, 'Tabletas', '500 mg');
crearLote(idCeros, `L-CERO-${SUFIJO}`, 'DATE_ADD(CURDATE(), INTERVAL 12 MONTH)', 0);

const idCaducado = crearMedicamento(SOLO_CADUCADO, 'Jarabe', '250 mg');
crearLote(idCaducado, `L-CAD-${SUFIJO}`, 'DATE_SUB(CURDATE(), INTERVAL 3 DAY)', 30);

const idVigente = crearMedicamento(CON_EXISTENCIAS, 'Cápsulas', '100 mg');
crearLote(idVigente, `L-VIG-${SUFIJO}`, 'DATE_ADD(CURDATE(), INTERVAL 2 MONTH)', 8);

const idMismoAgotado = crearMedicamento(MISMO_NOMBRE, 'Tabletas', '500 mg');
const idMismoConStock = crearMedicamento(MISMO_NOMBRE, 'Jarabe', '120 mg/5 ml');
crearLote(idMismoConStock, `L-JAR-${SUFIJO}`, 'DATE_ADD(CURDATE(), INTERVAL 18 MONTH)', 20);

const existenciasEnBase = () =>
  consultar(`SELECT COALESCE(SUM(stock), 0) FROM lote WHERE id_medicamento IN (${idCeros}, ${idCaducado}, ${idVigente}, ${idMismoConStock})`);
const movimientosEnBase = () => consultar('SELECT COUNT(*) FROM movimiento');
const existenciasIniciales = existenciasEnBase();
const movimientosIniciales = movimientosEnBase();

const admin = await (async () => {
  const login = await fetch(`${API}/api/sesiones/iniciar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario: 'admin', contrasenia: CONTRASENIA_ADMIN }),
  });
  return login.headers.getSetCookie()[0].split(';')[0];
})();

await fetch(`${API}/api/usuarios`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json', Cookie: admin },
  body: JSON.stringify({
    nombre: 'Luis', apellidos: 'Prueba E2E', nombreUsuario: EMPLEADO, correo: `${EMPLEADO}@etbotfarma.test`,
    telefono: '5500000012', contrasenia: 'Farmacia#2026', idRol: 2,
  }),
});

async function iniciarSesion(navegador, usuario, contrasenia) {
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', usuario);
  await pagina.fill('#contrasenia', contrasenia);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });
  return { contexto, pagina };
}

async function abrirInventario(pagina) {
  await pagina.goto(`${URL}/inventario`);
  await pagina.getByRole('heading', { name: 'Inventario de medicamentos' }).waitFor({ timeout: 10000 });
  await pagina.getByRole('row').filter({ hasText: CON_EXISTENCIAS }).first().waitFor({ timeout: 10000 });
}

const tablaAgotados = (pagina) => pagina.getByRole('table', { name: 'Medicamentos agotados' });
const filaAgotada = (pagina, nombre) => tablaAgotados(pagina).getByRole('row').filter({ hasText: nombre });

async function abrirAgotados(pagina) {
  await pagina.getByRole('button', { name: 'Agotados', exact: true }).click();
  await tablaAgotados(pagina).waitFor({ timeout: 10000 });
}

const navegador = await chromium.launch();

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/inventario`);
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 10000 }).catch(() => {});
  revisar('Sin sesión, la dirección del inventario manda al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  await contexto.close();
}

{
  const respuesta = await fetch(`${API}/api/inventario/agotados`);
  revisar('Sin sesión, el servidor rechaza la consulta con 401', respuesta.status === 401);
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, 'admin', CONTRASENIA_ADMIN);
  await abrirInventario(pagina);

  const filtroAgotados = pagina.getByRole('button', { name: 'Agotados', exact: true });
  revisar('Existe el filtro "Agotados" como opción independiente', (await filtroAgotados.count()) === 1);
  revisar(
    'Los filtros de caducidad siguen disponibles',
    (await pagina.getByRole('button', { name: 'Todos', exact: true }).count()) === 1 &&
      (await pagina.getByRole('button', { name: /Rojo/ }).count()) === 1,
  );

  await abrirAgotados(pagina);
  const textoCeros = await filaAgotada(pagina, SIN_LOTES_EN_CERO).first().innerText();
  revisar('Medicamento con todos sus lotes en cero aparece como agotado', textoCeros.includes('SIN EXISTENCIAS'));
  revisar('Muestra su presentación y concentración', textoCeros.includes('Tabletas') && textoCeros.includes('500 mg'));
  revisar('Medicamento con solo lotes caducados aparece como agotado', (await filaAgotada(pagina, SOLO_CADUCADO).count()) === 1);
  revisar('Medicamento con un lote rojo que aún no caduca no aparece', (await filaAgotada(pagina, CON_EXISTENCIAS).count()) === 0);
  revisar(
    'Mismo nombre: solo aparece la presentación agotada',
    (await filaAgotada(pagina, MISMO_NOMBRE).count()) === 1 && (await filaAgotada(pagina, MISMO_NOMBRE).first().innerText()).includes('Tabletas'),
  );
  await pagina.screenshot({ path: `${CARPETA}/hu12-agotados.png` });

  const buscador = pagina.getByLabel('Buscar medicamento');
  await buscador.fill(SOLO_CADUCADO);
  revisar(
    'La búsqueda se combina con el filtro Agotados',
    (await filaAgotada(pagina, SOLO_CADUCADO).count()) === 1 && (await filaAgotada(pagina, SIN_LOTES_EN_CERO).count()) === 0,
  );
  await buscador.fill(`ZZZ-${SUFIJO}`);
  revisar('Si nada coincide se informa que no hay resultados', await aparece(pagina, 'No hay medicamentos agotados que coincidan con la búsqueda.'));
  await buscador.fill('');

  await pagina.getByRole('button', { name: 'Todos', exact: true }).click();
  await pagina.getByRole('row').filter({ hasText: CON_EXISTENCIAS }).first().waitFor({ timeout: 10000 });
  revisar('Al cambiar a Todos se ve la lista completa del inventario', (await tablaAgotados(pagina).count()) === 0);
  revisar('No se pidió confirmación al cambiar de filtro', (await pagina.getByRole('dialog').count()) === 0);

  await abrirAgotados(pagina);
  revisar('Al volver a Agotados se muestra de nuevo su lista', (await filaAgotada(pagina, SIN_LOTES_EN_CERO).count()) === 1);

  await pagina.route('**/api/inventario/agotados**', (ruta) => ruta.abort());
  await pagina.getByRole('button', { name: 'Todos', exact: true }).click();
  await pagina.getByRole('button', { name: 'Agotados', exact: true }).click();
  revisar('Si falla la carga se muestra una alerta', await aparece(pagina, 'No se pudo cargar la lista de medicamentos agotados.'));
  revisar('La alerta permite reintentar', (await pagina.getByRole('button', { name: 'Reintentar' }).count()) === 1);
  await pagina.screenshot({ path: `${CARPETA}/hu12-error.png` });

  await pagina.unroute('**/api/inventario/agotados**');
  await pagina.getByRole('button', { name: 'Reintentar' }).click();
  await tablaAgotados(pagina).waitFor({ timeout: 10000 });
  revisar('Al reintentar vuelve a mostrarse la lista', (await filaAgotada(pagina, SIN_LOTES_EN_CERO).count()) === 1);

  await contexto.close();
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, EMPLEADO, 'Farmacia#2026');
  await abrirInventario(pagina);
  await abrirAgotados(pagina);
  revisar('El Empleado también ve los medicamentos agotados', (await filaAgotada(pagina, SIN_LOTES_EN_CERO).count()) === 1);
  await pagina.screenshot({ path: `${CARPETA}/hu12-empleado.png` });
  await contexto.close();
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, 'admin', CONTRASENIA_ADMIN);
  await abrirInventario(pagina);
  await abrirAgotados(pagina);
  await pagina.getByRole('button', { name: 'Todos', exact: true }).click();
  await abrirAgotados(pagina);
  revisar('Consultar varias veces no cambia las existencias', existenciasEnBase() === existenciasIniciales);
  revisar('Consultar varias veces no registra movimientos en la bitácora', movimientosEnBase() === movimientosIniciales);
  await contexto.close();
}

consultar(`INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idCeros}, 'L-ENTRADA-${SUFIJO}', DATE_ADD(CURDATE(), INTERVAL 12 MONTH), 15, CURDATE());`);

{
  const { contexto, pagina } = await iniciarSesion(navegador, 'admin', CONTRASENIA_ADMIN);
  await abrirInventario(pagina);
  await abrirAgotados(pagina);
  revisar('Con una entrada con unidades deja de aparecer como agotado', (await filaAgotada(pagina, SIN_LOTES_EN_CERO).count()) === 0);
  revisar('Los demás agotados siguen en la lista', (await filaAgotada(pagina, SOLO_CADUCADO).count()) === 1);
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-12 en navegador: todas las pruebas pasaron' : `HU-12 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
