import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const CONTRASENIA_ADMIN = process.env.CONTRASENIA ?? 'Prueba123!';
const MYSQL = process.env.MYSQL ?? 'mysql -h127.0.0.1 -uroot -proot -N etbotfarma';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

const SUFIJO = Date.now().toString(36);
const NOMBRE = `E2E Salida ${SUFIJO}`;
const NOMBRE_VENCIDO = `E2E Salida Vencido ${SUFIJO}`;

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
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE}', 'Sustancia E2E', 'Tabletas', '500 mg', 0, 0, 1, NOW()); SELECT LAST_INSERT_ID();`,
);
consultar(
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, 'L-LARGO-${SUFIJO}', DATE_ADD(CURDATE(), INTERVAL 24 MONTH), 30, DATE_SUB(CURDATE(), INTERVAL 2 MONTH));`,
);
consultar(
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, 'L-CORTO-${SUFIJO}', DATE_ADD(CURDATE(), INTERVAL 4 MONTH), 20, DATE_SUB(CURDATE(), INTERVAL 1 MONTH));`,
);
consultar(
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, 'L-VENCIDO-${SUFIJO}', DATE_SUB(CURDATE(), INTERVAL 5 DAY), 99, DATE_SUB(CURDATE(), INTERVAL 8 MONTH));`,
);
const idVencido = consultar(
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE_VENCIDO}', 'Sustancia E2E', 'Jarabe', '250 mg', 0, 0, 1, NOW()); SELECT LAST_INSERT_ID();`,
);
consultar(
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idVencido}, 'L-SOLO-VENCIDO-${SUFIJO}', DATE_SUB(CURDATE(), INTERVAL 3 DAY), 30, DATE_SUB(CURDATE(), INTERVAL 6 MONTH));`,
);

const stockDe = (numero) =>
  consultar(`SELECT stock FROM lote WHERE id_medicamento = ${idMedicamento} AND numero_lote = '${numero}-${SUFIJO}'`);
const salidasEnBase = () =>
  consultar(`SELECT COUNT(*) FROM movimiento WHERE tipo = 'SALIDA' AND id_medicamento = ${idMedicamento}`);

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

const filaDe = (pagina, nombre) => pagina.getByRole('row').filter({ hasText: nombre }).first();

async function abrirPantalla(pagina) {
  await pagina.goto(`${URL}/inventario/salida`);
  await pagina.getByRole('heading', { name: 'Salida de medicamentos' }).waitFor({ timeout: 10000 });
  await pagina.fill('#termino-salida', `E2E Salida`);
  await pagina.getByRole('button', { name: 'Buscar' }).click();
  await filaDe(pagina, NOMBRE).waitFor({ timeout: 10000 });
}

async function seleccionar(pagina) {
  await filaDe(pagina, NOMBRE).getByRole('button', { name: 'Registrar salida' }).click();
  await pagina.getByRole('heading', { name: NOMBRE }).waitFor({ timeout: 8000 });
}

async function pedirPropuesta(pagina, cantidad) {
  await pagina.fill('#cantidad-salida', cantidad);
  await pagina.getByRole('button', { name: 'Ver propuesta de lotes' }).click();
}

const navegador = await chromium.launch();

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/inventario/salida`);
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 10000 }).catch(() => {});
  revisar('Sin sesión, la dirección directa manda al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  await contexto.close();
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, 'admin', CONTRASENIA_ADMIN);
  await abrirPantalla(pagina);

  const textoFila = await filaDe(pagina, NOMBRE).innerText();
  revisar('La lista muestra solo las existencias vigentes (50, sin el lote vencido)', textoFila.includes('50') && !textoFila.includes('149'));
  revisar(
    'Un medicamento con solo lotes vencidos no permite registrar salida',
    await filaDe(pagina, NOMBRE_VENCIDO).getByRole('button', { name: 'Registrar salida' }).isDisabled(),
  );

  await seleccionar(pagina);
  const tablaLotes = await pagina.getByRole('table').innerText();
  revisar('Muestra los lotes vigentes', tablaLotes.includes(`L-CORTO-${SUFIJO}`) && tablaLotes.includes(`L-LARGO-${SUFIJO}`));
  revisar('No muestra el lote vencido', !tablaLotes.includes(`L-VENCIDO-${SUFIJO}`));
  revisar('El lote que caduca antes aparece primero', tablaLotes.indexOf(`L-CORTO-${SUFIJO}`) < tablaLotes.indexOf(`L-LARGO-${SUFIJO}`));

  await pedirPropuesta(pagina, '');
  revisar('Cantidad vacía muestra Este campo es obligatorio', await aparece(pagina, 'Este campo es obligatorio', 3000));

  for (const invalida of ['0', '-3', '2.5', 'abc']) {
    await pedirPropuesta(pagina, invalida);
    revisar(
      `Cantidad "${invalida}" muestra el mensaje de entero mayor que 0`,
      await aparece(pagina, 'La cantidad debe ser un número entero mayor que 0', 3000),
    );
  }
  revisar('Sin cantidad válida no se abre la confirmación', (await pagina.getByRole('dialog').count()) === 0);

  await pedirPropuesta(pagina, '999');
  revisar('Cantidad mayor al stock muestra Stock insuficiente', await aparece(pagina, 'Stock insuficiente'));
  revisar('Stock insuficiente no cambia ningún lote', stockDe('L-CORTO') === '20' && stockDe('L-LARGO') === '30');

  await pedirPropuesta(pagina, '25');
  const dialogo = pagina.getByRole('dialog');
  await dialogo.waitFor({ timeout: 8000 });
  const textoDialogo = await dialogo.innerText();
  revisar('La propuesta toma 20 del lote que caduca antes', textoDialogo.includes(`L-CORTO-${SUFIJO}`) && textoDialogo.includes(': 20'));
  revisar('La propuesta toma 5 del siguiente lote', textoDialogo.includes(`L-LARGO-${SUFIJO}`) && textoDialogo.includes(': 5'));
  revisar('Ver la propuesta no cambia el stock', stockDe('L-CORTO') === '20' && stockDe('L-LARGO') === '30' && salidasEnBase() === '0');

  await dialogo.getByRole('button', { name: 'Cancelar' }).click();
  await pagina.waitForTimeout(500);
  revisar('Cancelar no registra nada', stockDe('L-CORTO') === '20' && salidasEnBase() === '0');

  await pedirPropuesta(pagina, '25');
  await dialogo.waitFor({ timeout: 8000 });
  await dialogo.getByRole('button', { name: 'Confirmar salida' }).click();
  revisar('Al confirmar muestra Salida registrada', await aparece(pagina, 'Salida registrada'));
  revisar('El lote que caduca antes quedó en 0', stockDe('L-CORTO') === '0');
  revisar('El segundo lote quedó en 25', stockDe('L-LARGO') === '25');
  revisar('El lote vencido no se tocó', stockDe('L-VENCIDO') === '99');
  revisar('Se guardó un solo movimiento de salida', salidasEnBase() === '1');
  revisar(
    'El movimiento tiene un detalle por lote usado',
    consultar(
      `SELECT COUNT(*) FROM detalle_movimiento d JOIN movimiento m ON m.id_movimiento = d.id_movimiento WHERE m.tipo = 'SALIDA' AND m.id_medicamento = ${idMedicamento}`,
    ) === '2',
  );

  await pagina.screenshot({ path: `${CARPETA}/hu13-salida-registrada.png` });
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-13 en navegador: todas las pruebas pasaron' : `HU-13 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
