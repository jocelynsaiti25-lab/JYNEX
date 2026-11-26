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
const NOMBRE = `E2E Stock ${SUFIJO}`;
const NOMBRE_CADUCADO = `E2E Stock Caducado ${SUFIJO}`;
const LUIS = `e2e.stock.${SUFIJO}`;

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
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, 'LOTE-E2E-${SUFIJO}', DATE_ADD(CURDATE(), INTERVAL 24 MONTH), 40, CURDATE());`,
);
const idCaducado = consultar(
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE_CADUCADO}', 'Sustancia E2E', 'Jarabe', '250 mg', 0, 5, 1, NOW()); SELECT LAST_INSERT_ID();`,
);
consultar(
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idCaducado}, 'LOTE-CAD-${SUFIJO}', DATE_SUB(CURDATE(), INTERVAL 3 DAY), 30, DATE_SUB(CURDATE(), INTERVAL 6 MONTH));`,
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
    telefono: '5500000011', contrasenia: 'Farmacia#2026', idRol: 2,
  }),
});

const stockMinimoEnBase = () => consultar(`SELECT stock_minimo FROM medicamento WHERE id_medicamento = ${idMedicamento}`);

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

async function abrirPantalla(pagina) {
  await pagina.goto(`${URL}/inventario/stock-minimo`);
  await pagina.getByRole('heading', { name: 'Control de stock mínimo' }).waitFor({ timeout: 10000 });
  await pagina.getByRole('row').filter({ hasText: NOMBRE }).first().waitFor({ timeout: 10000 });
}

const filaDe = (pagina, nombre) => pagina.getByRole('row').filter({ hasText: nombre }).first();

async function guardarCambio(pagina, fila, valor) {
  await fila.locator('input').fill(valor);
  await fila.getByRole('button', { name: 'Guardar' }).click();
  const dialogo = pagina.getByRole('dialog');
  await dialogo.getByRole('button', { name: 'Guardar' }).click();
}

const navegador = await chromium.launch();

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/inventario/stock-minimo`);
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 10000 }).catch(() => {});
  revisar('Sin sesión, la dirección directa manda al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  await contexto.close();
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, 'admin', CONTRASENIA_ADMIN);
  await abrirPantalla(pagina);
  const fila = filaDe(pagina, NOMBRE);
  const entrada = fila.locator('input');

  revisar('El Administrador ve las existencias vigentes del medicamento', (await fila.innerText()).includes('40'));
  revisar('Muestra el stock mínimo actual en el campo', (await entrada.inputValue()) === '0');
  revisar('Sin cambios no aparece el botón Guardar', (await fila.getByRole('button', { name: 'Guardar' }).count()) === 0);

  const filaCaducada = filaDe(pagina, NOMBRE_CADUCADO);
  const textoCaducada = await filaCaducada.innerText();
  revisar('Un medicamento con todos los lotes caducados aparece como AGOTADO', textoCaducada.includes('AGOTADO'));
  revisar('Y no oculta el aviso de lotes caducados con sus unidades', textoCaducada.includes('LOTES CADUCADOS (30)'));

  const invalidos = [
    ['', 'El stock mínimo es obligatorio.', 'campo vacío'],
    ['   ', 'El stock mínimo es obligatorio.', 'solo espacios'],
    ['-3', 'El stock mínimo no puede ser negativo.', 'número negativo'],
    ['5.8', 'El stock mínimo debe ser un número entero, sin decimales.', 'número decimal'],
    ['5,8', 'El stock mínimo debe ser un número entero, sin decimales.', 'decimal con coma'],
    ['abc', 'El stock mínimo debe ser un número entero.', 'texto'],
    ['99999999999', 'El stock mínimo no puede ser mayor a 2147483647.', 'valor fuera de rango'],
  ];

  for (const [valor, mensaje, descripcion] of invalidos) {
    await entrada.fill(valor);
    revisar(`Muestra el mensaje para ${descripcion}`, await aparece(pagina, mensaje, 3000));
    revisar(`El botón Guardar queda deshabilitado con ${descripcion}`, await fila.getByRole('button', { name: 'Guardar' }).isDisabled());
    revisar(`El valor escrito para ${descripcion} no se altera en el campo`, (await entrada.inputValue()) === valor);
    revisar(`No se envió nada a la base con ${descripcion}`, stockMinimoEnBase() === '0');
  }

  await pagina.screenshot({ path: `${CARPETA}/hu11-error.png` });

  await entrada.fill('0');
  revisar('Volver al valor original oculta el error y el botón', (await fila.getByRole('button', { name: 'Guardar' }).count()) === 0);

  await entrada.fill('7');
  await fila.getByRole('button', { name: 'Guardar' }).click();
  const dialogo = pagina.getByRole('dialog');
  revisar('Pide confirmación antes de guardar', await aparece(pagina, '¿Guardar el nuevo stock mínimo?'));
  await dialogo.getByRole('button', { name: 'Cancelar' }).click();
  await pagina.waitForTimeout(500);
  revisar('Al cancelar la confirmación no se guarda nada', stockMinimoEnBase() === '0');

  await guardarCambio(pagina, filaDe(pagina, NOMBRE), '3');
  revisar('Confirma que el stock mínimo se actualizó', await aparece(pagina, 'Stock mínimo actualizado'));
  revisar('El nuevo valor queda guardado en MySQL', stockMinimoEnBase() === '3');
  revisar('El campo muestra el nuevo valor', (await filaDe(pagina, NOMBRE).locator('input').inputValue()) === '3');
  revisar('Con 40 vigentes y mínimo 3 sigue Disponible', (await filaDe(pagina, NOMBRE).innerText()).includes('Disponible'));

  await guardarCambio(pagina, filaDe(pagina, NOMBRE), '40');
  await filaDe(pagina, NOMBRE).getByText('STOCK BAJO').waitFor({ timeout: 8000 }).catch(() => {});
  revisar('Con vigentes iguales al mínimo aparece STOCK BAJO', (await filaDe(pagina, NOMBRE).innerText()).includes('STOCK BAJO'));
  revisar('El umbral 40 quedó guardado en MySQL', stockMinimoEnBase() === '40');

  await guardarCambio(pagina, filaDe(pagina, NOMBRE), '0');
  await pagina.waitForTimeout(800);
  revisar('Cero es un valor válido y se guarda', stockMinimoEnBase() === '0');

  await pagina.screenshot({ path: `${CARPETA}/hu11-administrador.png` });
  await contexto.close();
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, LUIS, 'Farmacia#2026');
  await abrirPantalla(pagina);
  revisar('El Empleado puede abrir la pantalla de stock mínimo', pagina.url().includes('/inventario/stock-minimo'));

  const entrada = filaDe(pagina, NOMBRE).locator('input');
  await entrada.fill('abc');
  revisar('El Empleado ve el mensaje de validación', await aparece(pagina, 'El stock mínimo debe ser un número entero.', 3000));
  revisar('El botón Guardar del Empleado queda deshabilitado con texto', await filaDe(pagina, NOMBRE).getByRole('button', { name: 'Guardar' }).isDisabled());

  await guardarCambio(pagina, filaDe(pagina, NOMBRE), '5');
  revisar('El Empleado recibe la confirmación de guardado', await aparece(pagina, 'Stock mínimo actualizado'));
  revisar('El valor del Empleado queda guardado en MySQL', stockMinimoEnBase() === '5');
  revisar('El Empleado ve el nuevo valor en el campo', (await filaDe(pagina, NOMBRE).locator('input').inputValue()) === '5');

  await pagina.screenshot({ path: `${CARPETA}/hu11-empleado.png` });
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-11 en navegador: todas las pruebas pasaron' : `HU-11 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
