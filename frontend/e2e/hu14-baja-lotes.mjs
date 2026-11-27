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
const NOMBRE = `E2E Baja ${SUFIJO}`;
const LOTE_ROJO = `R-${SUFIJO}`;
const LOTE_CADUCADO = `C-${SUFIJO}`;
const LOTE_AMARILLO = `A-${SUFIJO}`;
const LOTE_VERDE = `V-${SUFIJO}`;
const LOTE_LIMITE = `L3-${SUFIJO}`;
const LOTE_PASADO = `L3D-${SUFIJO}`;
const LUIS = `e2e.baja.${SUFIJO}`;

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

const idMedicamento = consultar(
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE}', 'Sustancia E2E', 'Tabletas', '500 mg', 0, 0, 1, NOW()); SELECT LAST_INSERT_ID();`,
);

function insertarLote(numero, caducidad, stock = 10) {
  consultar(
    `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, '${numero}', ${caducidad}, ${stock}, DATE_SUB(CURDATE(), INTERVAL 1 YEAR));`,
  );
}

insertarLote(LOTE_ROJO, 'DATE_ADD(CURDATE(), INTERVAL 2 MONTH)');
insertarLote(LOTE_CADUCADO, 'DATE_SUB(CURDATE(), INTERVAL 3 DAY)');
insertarLote(LOTE_AMARILLO, 'DATE_ADD(CURDATE(), INTERVAL 8 MONTH)');
insertarLote(LOTE_VERDE, 'DATE_ADD(CURDATE(), INTERVAL 24 MONTH)');
insertarLote(LOTE_LIMITE, 'DATE_ADD(CURDATE(), INTERVAL 3 MONTH)');
insertarLote(LOTE_PASADO, 'DATE_ADD(DATE_ADD(CURDATE(), INTERVAL 3 MONTH), INTERVAL 1 DAY)');

const stockDe = (lote) =>
  Number(consultar(`SELECT stock FROM lote WHERE id_medicamento = ${idMedicamento} AND numero_lote = '${lote}'`));

const bajasDe = (lote) =>
  Number(consultar(
    `SELECT COUNT(*) FROM movimiento m JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento JOIN lote l ON l.id_lote = d.id_lote WHERE l.id_medicamento = ${idMedicamento} AND l.numero_lote = '${lote}' AND m.tipo = 'BAJA'`,
  ));

const motivoDe = (lote) =>
  consultar(
    `SELECT m.motivo FROM movimiento m JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento JOIN lote l ON l.id_lote = d.id_lote WHERE l.id_medicamento = ${idMedicamento} AND l.numero_lote = '${lote}' AND m.tipo = 'BAJA'`,
  );

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
    nombre: 'Luis', apellidos: 'Prueba E2E', nombreUsuario: LUIS, correo: `${LUIS}@etbotfarma.test`,
    telefono: '5500000014', contrasenia: 'Farmacia#2026', idRol: 2,
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
  await pagina.getByLabel('Buscar medicamento').waitFor({ timeout: 10000 });
  await pagina.getByLabel('Buscar medicamento').fill(NOMBRE);
  await pagina.getByRole('row').filter({ hasText: LOTE_VERDE }).waitFor({ timeout: 10000 });
}

async function cumple(condicion, tiempo = 5000) {
  const limite = Date.now() + tiempo;
  while (Date.now() < limite) {
    if (await condicion()) return true;
    await new Promise((resolver) => setTimeout(resolver, 100));
  }
  return condicion();
}

async function aparece(pagina, texto, tiempo = 5000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

const filaDe = (pagina, lote) => pagina.getByRole('row').filter({ hasText: lote });
const botonBaja = (pagina, lote) => filaDe(pagina, lote).getByRole('button', { name: /Dar de baja/ });
const dialogoMotivo = (pagina) => pagina.getByRole('dialog', { name: 'Dar de baja el lote', exact: true });
const dialogoConfirmar = (pagina) => pagina.getByRole('dialog', { name: '¿Dar de baja el lote?' });

async function llegarAConfirmacion(pagina, lote, motivo) {
  await botonBaja(pagina, lote).click();
  await dialogoMotivo(pagina).waitFor({ timeout: 5000 });
  await pagina.locator('#motivo-baja').fill(motivo);
  await dialogoMotivo(pagina).getByRole('button', { name: 'Continuar' }).click();
  await dialogoConfirmar(pagina).waitFor({ timeout: 5000 });
}

const navegador = await chromium.launch();

{
  const { contexto, pagina } = await iniciarSesion(navegador, 'admin', CONTRASENIA_ADMIN);
  await abrirInventario(pagina);

  revisar('Lote rojo: muestra el botón Dar de baja', (await botonBaja(pagina, LOTE_ROJO).count()) === 1);
  revisar('Lote caducado: muestra el botón Dar de baja', (await botonBaja(pagina, LOTE_CADUCADO).count()) === 1);
  revisar('Lote amarillo: no muestra el botón', (await botonBaja(pagina, LOTE_AMARILLO).count()) === 0);
  revisar('Lote verde: no muestra el botón', (await botonBaja(pagina, LOTE_VERDE).count()) === 0);
  revisar('Lote que caduca exactamente en 3 meses: es rojo y muestra el botón', (await botonBaja(pagina, LOTE_LIMITE).count()) === 1);
  revisar('Lote de 3 meses y un día: no muestra el botón', (await botonBaja(pagina, LOTE_PASADO).count()) === 0);

  await botonBaja(pagina, LOTE_ROJO).click();
  await dialogoMotivo(pagina).waitFor({ timeout: 5000 });
  await dialogoMotivo(pagina).getByRole('button', { name: 'Continuar' }).click();
  revisar('Motivo vacío: muestra "El motivo es obligatorio"', await aparece(pagina, 'El motivo es obligatorio'));
  revisar('Motivo vacío: no abre la confirmación', (await dialogoConfirmar(pagina).count()) === 0);

  await pagina.locator('#motivo-baja').fill('     ');
  await dialogoMotivo(pagina).getByRole('button', { name: 'Continuar' }).click();
  revisar('Motivo con solo espacios: sigue sin abrir la confirmación', (await dialogoConfirmar(pagina).count()) === 0);

  await pagina.locator('#motivo-baja').fill('x'.repeat(201));
  await dialogoMotivo(pagina).getByRole('button', { name: 'Continuar' }).click();
  revisar('Motivo de 201 caracteres: muestra el límite de 200', await aparece(pagina, 'El motivo no puede tener más de 200 caracteres'));
  revisar('Motivo de 201 caracteres: no abre la confirmación', (await dialogoConfirmar(pagina).count()) === 0);

  await pagina.locator('#motivo-baja').fill('Próximo a caducar');
  await dialogoMotivo(pagina).getByRole('button', { name: 'Continuar' }).click();
  await dialogoConfirmar(pagina).waitFor({ timeout: 5000 });
  const resumen = await dialogoConfirmar(pagina).innerText();
  revisar('La confirmación muestra el medicamento', resumen.includes(NOMBRE));
  revisar('La confirmación muestra el número de lote', resumen.includes(LOTE_ROJO));
  revisar('La confirmación muestra la cantidad a retirar', resumen.includes('10 unidades'));
  revisar('La confirmación muestra el motivo', resumen.includes('Próximo a caducar'));
  revisar('La confirmación advierte que no se puede deshacer', resumen.includes('no se puede deshacer'));
  await pagina.screenshot({ path: `${CARPETA}/hu14-confirmacion.png` });

  await dialogoConfirmar(pagina).getByRole('button', { name: 'Cancelar' }).click();
  revisar('Cancelar: cierra la confirmación', await cumple(async () => (await dialogoConfirmar(pagina).count()) === 0));
  revisar('Cancelar: el lote conserva sus existencias', stockDe(LOTE_ROJO) === 10);
  revisar('Cancelar: no se registra ninguna baja', bajasDe(LOTE_ROJO) === 0);

  await llegarAConfirmacion(pagina, LOTE_ROJO, 'Próximo a caducar');
  await dialogoConfirmar(pagina).getByRole('button', { name: 'Confirmar' }).dblclick();
  revisar('Baja exitosa: muestra "Lote dado de baja"', await aparece(pagina, 'Lote dado de baja'));
  revisar('Baja exitosa: las existencias del lote quedan en 0', stockDe(LOTE_ROJO) === 0);
  revisar('Doble clic en Confirmar: la baja se registra una sola vez', bajasDe(LOTE_ROJO) === 1);
  revisar('La bitácora guarda el motivo', motivoDe(LOTE_ROJO) === 'Próximo a caducar');
  revisar('La pantalla se actualiza sin recargar: el lote ya no aparece con botón',
    await cumple(async () => (await botonBaja(pagina, LOTE_ROJO).count()) === 0));
  revisar('Los demás lotes conservan sus existencias', stockDe(LOTE_VERDE) === 10 && stockDe(LOTE_AMARILLO) === 10);
  await pagina.screenshot({ path: `${CARPETA}/hu14-baja-exitosa.png` });

  await llegarAConfirmacion(pagina, LOTE_CADUCADO, 'Caducado');
  consultar(`UPDATE lote SET stock = 7 WHERE id_medicamento = ${idMedicamento} AND numero_lote = '${LOTE_CADUCADO}'`);
  await dialogoConfirmar(pagina).getByRole('button', { name: 'Confirmar' }).click();
  revisar('Lote que cambió antes de confirmar: muestra "El lote ya no es elegible"', await aparece(pagina, 'El lote ya no es elegible'));
  revisar('Lote que cambió: no se aplica la baja', stockDe(LOTE_CADUCADO) === 7 && bajasDe(LOTE_CADUCADO) === 0);
  revisar('Lote que cambió: la lista muestra las existencias reales',
    await cumple(async () => (await filaDe(pagina, LOTE_CADUCADO).locator('td').nth(4).innerText()).trim() === '7'));

  await pagina.route('**/api/inventario/lotes/*/baja', (ruta) => ruta.abort());
  await llegarAConfirmacion(pagina, LOTE_LIMITE, 'Próximo a caducar');
  await dialogoConfirmar(pagina).getByRole('button', { name: 'Confirmar' }).click();
  revisar('Sin conexión: muestra "No se pudo dar de baja el lote"', await aparece(pagina, 'No se pudo dar de baja el lote'));
  revisar('Sin conexión: el lote conserva sus existencias', stockDe(LOTE_LIMITE) === 10 && bajasDe(LOTE_LIMITE) === 0);
  await pagina.unroute('**/api/inventario/lotes/*/baja');

  await contexto.close();
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, LUIS, 'Farmacia#2026');
  await abrirInventario(pagina);
  revisar('El Empleado ve el botón Dar de baja en el lote caducado', (await botonBaja(pagina, LOTE_CADUCADO).count()) === 1);

  await llegarAConfirmacion(pagina, LOTE_CADUCADO, 'Caducado');
  await dialogoConfirmar(pagina).getByRole('button', { name: 'Confirmar' }).click();
  revisar('El Empleado completa la baja del lote caducado', await aparece(pagina, 'Lote dado de baja'));
  revisar('Lote caducado: existencias en 0 y baja en la bitácora', stockDe(LOTE_CADUCADO) === 0 && bajasDe(LOTE_CADUCADO) === 1);
  await contexto.close();
}

{
  const contexto = await navegador.newContext();
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/inventario`);
  revisar('Sin sesión: la pantalla regresa al inicio de sesión',
    await cumple(async () => pagina.url().includes('/iniciar-sesion'), 10000));
  const respuesta = await fetch(`${API}/api/inventario/lotes/1/baja`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ cantidadConsultada: 1, motivo: 'Caducado' }),
  });
  revisar('Sin sesión: el servidor responde 401', respuesta.status === 401);
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-14 en navegador: todas las pruebas pasaron' : `HU-14 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
