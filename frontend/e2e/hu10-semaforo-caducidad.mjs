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
const NOMBRE = `E2E Semaforo ${SUFIJO}`;
const NOMBRE_OTRO = `E2E Semaforo Otro ${SUFIJO}`;
const LOTE_CADUCADO = `LOTE-CAD-${SUFIJO}`;
const LOTE_ROJO = `LOTE-ROJO-${SUFIJO}`;
const LOTE_AMARILLO = `LOTE-AMA-${SUFIJO}`;
const LOTE_VERDE = `LOTE-VER-${SUFIJO}`;
const LOTE_OTRO = `LOTE-OTRO-${SUFIJO}`;
const LUIS = `e2e.semaforo.${SUFIJO}`;

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

function insertarMedicamento(nombre) {
  return consultar(
    `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${nombre}', 'Sustancia E2E', 'Tabletas', '500 mg', 0, 0, 1, NOW()); SELECT LAST_INSERT_ID();`,
  );
}

function insertarLote(idMedicamento, numero, intervalo) {
  consultar(
    `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, '${numero}', ${intervalo}, 10, DATE_SUB(CURDATE(), INTERVAL 1 YEAR));`,
  );
}

const idMedicamento = insertarMedicamento(NOMBRE);
insertarLote(idMedicamento, LOTE_CADUCADO, 'DATE_SUB(CURDATE(), INTERVAL 3 DAY)');
insertarLote(idMedicamento, LOTE_ROJO, 'DATE_ADD(CURDATE(), INTERVAL 2 MONTH)');
insertarLote(idMedicamento, LOTE_AMARILLO, 'DATE_ADD(CURDATE(), INTERVAL 8 MONTH)');
insertarLote(idMedicamento, LOTE_VERDE, 'DATE_ADD(CURDATE(), INTERVAL 24 MONTH)');
const idOtro = insertarMedicamento(NOMBRE_OTRO);
insertarLote(idOtro, LOTE_OTRO, 'DATE_ADD(CURDATE(), INTERVAL 24 MONTH)');

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

const buscador = (pagina) => pagina.getByLabel('Buscar medicamento');
const filaDe = (pagina, lote) => pagina.getByRole('row').filter({ hasText: lote });
const filtro = (pagina, nombre) => pagina.getByRole('button', { name: nombre });
const FILTRO_ROJO = 'Próximos a caducar (Rojo)';
const FILTRO_AMARILLO = 'Precaución (Amarillo)';
const FILTRO_VERDE = 'Vigentes (Verde)';

async function esperarFilas(pagina, esperadas, patron = SUFIJO) {
  const filas = pagina.getByRole('row').filter({ hasText: patron });
  for (let intento = 0; intento < 30; intento++) {
    if ((await filas.count()) === esperadas) return true;
    await pagina.waitForTimeout(100);
  }
  return (await filas.count()) === esperadas;
}

async function aparece(pagina, texto, tiempo = 4000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

const esRojo = (color) => {
  const partes = color.match(/\d+(\.\d+)?/g)?.map(Number) ?? [];
  const [r, g, b] = partes;
  return r > 150 && r > g + 50 && r > b + 50;
};

const navegador = await chromium.launch();

{
  const { contexto, pagina } = await iniciarSesion(navegador, 'admin', CONTRASENIA_ADMIN);
  await pagina.goto(`${URL}/inventario`);
  await buscador(pagina).waitFor({ timeout: 10000 });

  await buscador(pagina).fill(SUFIJO);
  revisar('Búsqueda sin filtro: muestra los 4 lotes del medicamento y el lote del otro', await esperarFilas(pagina, 5));

  await buscador(pagina).fill(NOMBRE);
  revisar('Buscar por nombre muestra solo los 4 lotes de ese medicamento', await esperarFilas(pagina, 4));

  const caducado = filaDe(pagina, LOTE_CADUCADO);
  const rojo = filaDe(pagina, LOTE_ROJO);
  const amarillo = filaDe(pagina, LOTE_AMARILLO);
  const verde = filaDe(pagina, LOTE_VERDE);

  revisar('El lote caducado muestra la etiqueta CADUCADO', (await caducado.innerText()).includes('CADUCADO'));
  revisar('El lote próximo a caducar no se marca como CADUCADO', !(await rojo.innerText()).includes('CADUCADO'));
  revisar('El lote próximo a caducar se muestra en Rojo', (await rojo.innerText()).includes('Rojo'));
  revisar('El lote amarillo se muestra en Amarillo y sin CADUCADO',
    (await amarillo.innerText()).includes('Amarillo') && !(await amarillo.innerText()).includes('CADUCADO'));
  revisar('El lote vigente se muestra en Verde y sin CADUCADO',
    (await verde.innerText()).includes('Verde') && !(await verde.innerText()).includes('CADUCADO'));

  const colorCaducado = await caducado.locator('.semaforo--caducado').evaluate((e) => getComputedStyle(e).color);
  const colorRojo = await rojo.locator('.semaforo--rojo').evaluate((e) => getComputedStyle(e).color);
  revisar('La etiqueta CADUCADO es de color rojo', esRojo(colorCaducado));
  revisar('CADUCADO usa el mismo rojo que el semáforo Rojo', colorCaducado === colorRojo);
  await pagina.screenshot({ path: `${CARPETA}/hu10-semaforo.png` });

  await filtro(pagina, FILTRO_ROJO).click();
  revisar('Filtro Rojo: muestra el lote caducado y el próximo a caducar (2 lotes)', await esperarFilas(pagina, 2, NOMBRE));
  revisar('Filtro Rojo: incluye el lote caducado', (await filaDe(pagina, LOTE_CADUCADO).count()) === 1);
  revisar('Filtro Rojo: no incluye los lotes amarillo ni verde',
    (await filaDe(pagina, LOTE_AMARILLO).count()) === 0 && (await filaDe(pagina, LOTE_VERDE).count()) === 0);
  revisar('Filtro Rojo: el botón queda marcado como activo',
    await filtro(pagina, FILTRO_ROJO).evaluate((e) => e.classList.contains('activo')));
  revisar('Filtro Rojo: se conserva el término escrito', (await buscador(pagina).inputValue()) === NOMBRE);

  await filtro(pagina, FILTRO_VERDE).click();
  revisar('Filtro Verde: muestra solo el lote vigente y no el caducado',
    (await esperarFilas(pagina, 1, NOMBRE)) && (await filaDe(pagina, LOTE_VERDE).count()) === 1
    && (await filaDe(pagina, LOTE_CADUCADO).count()) === 0);

  await filtro(pagina, FILTRO_AMARILLO).click();
  revisar('Cambiar de filtro sin perder el término: Amarillo muestra solo su lote',
    (await esperarFilas(pagina, 1, NOMBRE)) && (await filaDe(pagina, LOTE_AMARILLO).count()) === 1);

  await buscador(pagina).fill(LOTE_CADUCADO);
  await filtro(pagina, FILTRO_ROJO).click();
  revisar('Búsqueda por lote y filtro Rojo combinados: solo el lote caducado',
    (await esperarFilas(pagina, 1, LOTE_CADUCADO)) && (await filaDe(pagina, LOTE_CADUCADO).innerText()).includes('CADUCADO'));

  await filtro(pagina, FILTRO_VERDE).click();
  revisar('Búsqueda por lote caducado con filtro Verde: sin resultados',
    await aparece(pagina, `No hay lotes en la categoría "Verde" que coincidan con la búsqueda.`));
  revisar('Sin resultados muestra el aviso y no una tabla', (await pagina.getByText('No se encontraron coincidencias').count()) === 1);
  revisar('Sin resultados no muestra errores en pantalla', (await pagina.getByRole('alert').count()) === 0);

  await buscador(pagina).fill(NOMBRE);
  await filtro(pagina, FILTRO_ROJO).click();
  await esperarFilas(pagina, 2, NOMBRE);
  await pagina.getByRole('button', { name: 'Limpiar' }).click();
  revisar('Limpiar borra el término y conserva el filtro seleccionado',
    (await buscador(pagina).inputValue()) === ''
    && await filtro(pagina, FILTRO_ROJO).evaluate((e) => e.classList.contains('activo')));

  await filtro(pagina, 'Todos').click();
  await buscador(pagina).fill(SUFIJO);
  revisar('Restablecer con Todos vuelve a mostrar todos los lotes', await esperarFilas(pagina, 5));

  await buscador(pagina).fill(`NoExiste-${SUFIJO}`);
  revisar('Una búsqueda sin coincidencias muestra el aviso', await aparece(pagina, 'No hay medicamentos ni lotes que coincidan con la búsqueda.'));

  await contexto.close();
}

{
  const { contexto, pagina } = await iniciarSesion(navegador, LUIS, 'Farmacia#2026');
  await pagina.goto(`${URL}/inventario`);
  await buscador(pagina).fill(NOMBRE);
  revisar('El Empleado también ve los 4 lotes', await esperarFilas(pagina, 4));
  revisar('El Empleado ve la etiqueta CADUCADO en el lote caducado', (await filaDe(pagina, LOTE_CADUCADO).innerText()).includes('CADUCADO'));
  revisar('El Empleado no ve CADUCADO en el lote vigente', !(await filaDe(pagina, LOTE_VERDE).innerText()).includes('CADUCADO'));
  await filtro(pagina, FILTRO_ROJO).click();
  revisar('El Empleado puede usar el filtro Rojo', await esperarFilas(pagina, 2, NOMBRE));
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-10 en navegador: todas las pruebas pasaron' : `HU-10 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
