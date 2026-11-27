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
const NOMBRE_A = `Exporta A ${SUFIJO}`;
const NOMBRE_B = `Exporta B ${SUFIJO}`;
const MOTIVO = '=2+2 Envase dañado ñandú';

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

function fechaEn(meses) {
  const fecha = new Date();
  fecha.setUTCMonth(fecha.getUTCMonth() + meses);
  return fecha.toISOString().slice(0, 10);
}

async function api(cookie, metodo, ruta, cuerpo) {
  const respuesta = await fetch(`${API}${ruta}`, {
    method: metodo,
    headers: { 'Content-Type': 'application/json', ...(cookie ? { Cookie: cookie } : {}) },
    body: cuerpo ? JSON.stringify(cuerpo) : undefined,
  });
  const texto = await respuesta.text();
  return { estado: respuesta.status, json: texto ? JSON.parse(texto) : null, respuesta };
}

async function sesionApi(nombreUsuario, contrasenia) {
  const { respuesta } = await api(null, 'POST', '/api/sesiones/iniciar', { nombreUsuario, contrasenia });
  return respuesta.headers.getSetCookie()[0].split(';')[0];
}

async function prepararDatos() {
  const cookie = await sesionApi('admin', CONTRASENIA_ADMIN);

  const alta = async (nombreComercial, cantidadInicial) => {
    const { estado, json } = await api(cookie, 'POST', '/api/medicamentos', {
      nombreComercial, nombreSustancia: 'Sustancia E2E', presentacion: 'Tabletas', concentracion: '500 mg',
      esControlado: false, numeroLote: 'L-A', fechaCaducidad: fechaEn(12), cantidadInicial,
    });
    if (estado !== 201) throw new Error(`No se pudo crear ${nombreComercial}: ${estado}`);
    return json.idMedicamento;
  };

  const idA = await alta(NOMBRE_A, '10');
  await alta(NOMBRE_B, '5');

  const lote = await api(cookie, 'POST', '/api/entradas-lote', {
    idMedicamento: idA, numeroLote: 'L-B', fechaCaducidad: fechaEn(24), cantidad: '10',
  });
  if (lote.estado !== 201) throw new Error(`No se pudo crear el lote B: ${lote.estado}`);

  const salida = await api(cookie, 'POST', '/api/salidas-medicamento', { idMedicamento: idA, cantidad: '15' });
  if (salida.estado !== 201) throw new Error(`No se pudo registrar la salida: ${salida.estado}`);

  consultar(`UPDATE lote SET fecha_caducidad = '2000-01-01' WHERE id_lote = ${lote.json.idLote}`);

  const baja = await api(cookie, 'POST', `/api/inventario/lotes/${lote.json.idLote}/baja`, {
    cantidadConsultada: 5, motivo: MOTIVO,
  });
  if (baja.estado !== 200) throw new Error(`No se pudo dar de baja el lote: ${baja.estado}`);
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

async function filas(pagina) {
  await pagina.waitForTimeout(800);
  if (!(await pagina.locator('table.tabla').isVisible())) return [];
  return pagina.locator('table.tabla tbody tr').allInnerTexts();
}

async function aplicar(pagina) {
  await pagina.getByRole('button', { name: 'Aplicar filtros' }).click();
  return filas(pagina);
}

function contenidoDe(ruta, parte) {
  return execSync(`unzip -p "${ruta}" ${parte}`).toString();
}

await prepararDatos();
const movimientosAntes = consultar('SELECT COUNT(*) FROM movimiento');
const navegador = await chromium.launch();

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 }, acceptDownloads: true });
  const pagina = await contexto.newPage();
  const descargas = [];
  pagina.on('download', (descarga) => descargas.push(descarga));

  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);
  await pagina.goto(`${URL}/bitacora`);
  await filas(pagina);

  revisar('1. La bitácora muestra el botón "Exportar a Excel"',
    await pagina.getByRole('button', { name: 'Exportar a Excel' }).isVisible());

  await pagina.selectOption('#filtro-medicamento', { label: `${NOMBRE_A} · Tabletas · 500 mg` });
  const deA = await aplicar(pagina);
  revisar('Preparación: el medicamento tiene 4 movimientos', deA.length === 4);

  await pagina.getByRole('button', { name: 'Exportar a Excel' }).click();
  revisar('2a. La confirmación indica el número de movimientos', await aparece(pagina, '4 movimientos'));
  const dialogo = await pagina.locator('dialog[open]').innerText();
  revisar('2b. La confirmación muestra los filtros activos', dialogo.includes(NOMBRE_A) && dialogo.includes('Todas las fechas'));
  await pagina.screenshot({ path: `${CARPETA}/hu16-1-confirmacion.png`, fullPage: true });

  await pagina.getByRole('button', { name: 'Cancelar' }).click();
  await pagina.waitForTimeout(500);
  revisar('11a. Cancelar no descarga ningún archivo', descargas.length === 0);
  revisar('11b. Cancelar conserva los filtros y la lista',
    (await pagina.locator('#filtro-medicamento option:checked').innerText()).includes(NOMBRE_A) && (await filas(pagina)).length === 4);

  await pagina.getByRole('button', { name: 'Exportar a Excel' }).click();
  await pagina.locator('dialog[open]').waitFor();
  await pagina.getByRole('button', { name: 'Confirmar' }).dblclick();
  await pagina.waitForTimeout(2500);
  revisar('3a. Confirmar descarga el archivo y muestra "Descarga iniciada"', descargas.length >= 1 && (await aparece(pagina, 'Descarga iniciada')));
  revisar('15. Los clics repetidos en Confirmar descargan un solo archivo', descargas.length === 1);

  const nombre = descargas[0]?.suggestedFilename() ?? '';
  revisar('5. El nombre es descriptivo y lleva la fecha de generación', /^bitacora-movimientos-\d{4}-\d{2}-\d{2}\.xlsx$/.test(nombre));

  const ruta = `${CARPETA}/${nombre || 'bitacora.xlsx'}`;
  await descargas[0].saveAs(ruta);
  const hoja = contenidoDe(ruta, 'xl/worksheets/sheet1.xml');
  const encabezados = ['Fecha', 'Hora', 'Usuario', 'Tipo de movimiento', 'Medicamento', 'Lote', 'Cantidad', 'Motivo'];
  revisar('4. La primera fila trae los encabezados', encabezados.every((e) => hoja.includes(`>${e}</t>`)));
  revisar('7/9. El archivo trae los 4 movimientos del filtro y coincide con el número indicado', (hoja.match(/<row /g) ?? []).length === 5);
  revisar('6. Las fechas y las cantidades son números, no texto', /<c r="A2" s="1"><v>\d+<\/v><\/c>/.test(hoja) && /<c r="G2"><v>5<\/v><\/c>/.test(hoja));
  revisar('12. Un motivo que empieza con "=" queda como texto y no como fórmula', !hoja.includes('<f>') && hoja.includes(`>${MOTIVO}</t>`));
  revisar('13. Los acentos y la ñ se conservan', hoja.includes('dañado ñandú'));
  revisar('18. La salida con varios lotes es una fila con el detalle de cada lote', hoja.includes('L-A (10); L-B (5)'));

  await pagina.selectOption('#filtro-tipo', 'BAJA');
  await aplicar(pagina);
  await pagina.getByRole('button', { name: 'Exportar a Excel' }).click();
  await pagina.locator('dialog[open]').waitFor();
  const [descargaBajas] = await Promise.all([
    pagina.waitForEvent('download'),
    pagina.getByRole('button', { name: 'Confirmar' }).click(),
  ]);
  await descargaBajas.saveAs(`${CARPETA}/hu16-bajas.xlsx`);
  const hojaBajas = contenidoDe(`${CARPETA}/hu16-bajas.xlsx`, 'xl/worksheets/sheet1.xml');
  revisar('7. Con filtros, el archivo solo incluye los movimientos filtrados',
    (hojaBajas.match(/<row /g) ?? []).length === 2 && hojaBajas.includes('>BAJA</t>') && !hojaBajas.includes('>SALIDA</t>'));

  await pagina.selectOption('#filtro-medicamento', { label: `${NOMBRE_B} · Tabletas · 500 mg` });
  await aplicar(pagina);
  const antesDeSinMovimientos = descargas.length;
  await pagina.getByRole('button', { name: 'Exportar a Excel' }).click();
  revisar('10a. Sin movimientos se informa antes de generar el archivo', await aparece(pagina, 'No hay movimientos para exportar.'));
  revisar('10b. Sin movimientos no se abre la confirmación ni se descarga nada',
    (await pagina.locator('dialog[open]').count()) === 0 && descargas.length === antesDeSinMovimientos);
  await pagina.screenshot({ path: `${CARPETA}/hu16-2-sin-movimientos.png`, fullPage: true });

  await contexto.close();
}

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 }, acceptDownloads: true });
  const pagina = await contexto.newPage();
  const descargas = [];
  pagina.on('download', (descarga) => descargas.push(descarga));

  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);
  await pagina.goto(`${URL}/bitacora`);
  await filas(pagina);

  await pagina.route('**/api/bitacora/movimientos/exportacion**', (ruta) => ruta.abort());
  await pagina.getByRole('button', { name: 'Exportar a Excel' }).click();
  await pagina.locator('dialog[open]').waitFor();
  await pagina.getByRole('button', { name: 'Confirmar' }).click();
  revisar('16a. Si falla la generación se muestra una alerta', await aparece(pagina, 'No se pudo generar el archivo'));
  revisar('16b. No se descarga ningún archivo dañado', descargas.length === 0);
  await pagina.screenshot({ path: `${CARPETA}/hu16-3-error.png`, fullPage: true });

  await pagina.unroute('**/api/bitacora/movimientos/exportacion**');
  await pagina.getByRole('button', { name: 'Intentar de nuevo' }).click();
  await pagina.locator('dialog[open]').waitFor();
  await pagina.getByRole('button', { name: 'Confirmar' }).click();
  await pagina.waitForTimeout(2500);
  revisar('16c. El usuario puede intentarlo de nuevo y la descarga funciona', descargas.length === 1);

  await contexto.close();
}

{
  const sinSesion = await fetch(`${API}/api/bitacora/movimientos/exportacion`);
  revisar('18. Sin sesión el servidor responde 401 y no entrega archivo',
    sinSesion.status === 401 && !(sinSesion.headers.get('content-type') ?? '').includes('spreadsheetml'));

  const empleado = `e2e.exporta.${SUFIJO}`;
  const cookieAdmin = await sesionApi('admin', CONTRASENIA_ADMIN);
  const crear = await api(cookieAdmin, 'POST', '/api/usuarios', {
    nombre: 'Prueba', apellidos: 'Exporta E2E', nombreUsuario: empleado, correo: `${empleado}@etbotfarma.test`,
    telefono: '5500000016', contrasenia: 'Farmacia#2026', idRol: 2,
  });
  const cookieEmpleado = await sesionApi(empleado, 'Farmacia#2026');
  const comoEmpleado = await fetch(`${API}/api/bitacora/movimientos/exportacion`, { headers: { Cookie: cookieEmpleado } });
  revisar('Solo Administrador: el Empleado recibe 403 al exportar', crear.estado === 201 && comoEmpleado.status === 403);
}

await navegador.close();

revisar('14. Exportar no genera movimientos nuevos', consultar('SELECT COUNT(*) FROM movimiento') === movimientosAntes);

console.log(fallas === 0 ? '\nTodas las comprobaciones de HU-16 pasaron.' : `\n${fallas} comprobación(es) de HU-16 fallaron.`);
process.exit(fallas === 0 ? 0 : 1);
