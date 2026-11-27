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
const EMPLEADO = `e2e.bitacora.${SUFIJO}`;
const NOMBRE_A = `Bitacora A ${SUFIJO}`;
const NOMBRE_B = `Bitacora B ${SUFIJO}`;

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
    cantidadConsultada: 5, motivo: 'Caducado E2E',
  });
  if (baja.estado !== 200) throw new Error(`No se pudo dar de baja el lote: ${baja.estado}`);

  const crear = await api(cookie, 'POST', '/api/usuarios', {
    nombre: 'Prueba', apellidos: 'Bitacora E2E', nombreUsuario: EMPLEADO, correo: `${EMPLEADO}@etbotfarma.test`,
    telefono: '5500000015', contrasenia: 'Farmacia#2026', idRol: 2,
  });
  if (crear.estado !== 201) throw new Error(`No se pudo crear el Empleado: ${crear.estado}`);
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

await prepararDatos();
const movimientosAntes = consultar('SELECT COUNT(*) FROM movimiento');
const navegador = await chromium.launch();

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);

  await pagina.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Bitácora', exact: true }).click();
  await pagina.waitForURL('**/bitacora');
  const todas = await filas(pagina);
  const encabezados = (await pagina.locator('table.tabla thead th').allInnerTexts()).map((e) => e.toUpperCase());
  revisar('1a. La opción "Bitácora" del menú abre la lista', todas.length >= 5);
  revisar('1b. Columnas: fecha, usuario, tipo, medicamento, lotes, cantidad y motivo',
    ['FECHA Y HORA', 'USUARIO', 'TIPO', 'MEDICAMENTO', 'LOTES', 'CANTIDAD', 'MOTIVO'].every((c) => encabezados.includes(c)));
  await pagina.screenshot({ path: `${CARPETA}/hu15-1-lista.png`, fullPage: true });

  await pagina.selectOption('#filtro-medicamento', { label: `${NOMBRE_A} · Tabletas · 500 mg` });
  const deA = await aplicar(pagina);
  revisar('11. El filtro por medicamento muestra sus 4 movimientos', deA.length === 4);
  revisar('6. El historial va del más reciente al más antiguo', deA[0].includes('BAJA') && deA[3].includes('ENTRADA'));
  revisar('4. La baja muestra su cantidad, lote y motivo', deA[0].includes('Caducado E2E') && deA[0].includes('L-B') && deA[0].includes('5'));
  const salidaTexto = deA[1];
  revisar('18. La salida muestra un solo movimiento con el detalle de ambos lotes',
    salidaTexto.includes('SALIDA') && salidaTexto.includes('L-A: 10') && salidaTexto.includes('L-B: 5') && salidaTexto.includes('15'));
  revisar('5. Entradas y salidas no muestran motivo', !deA[1].includes('FEFO') && !deA[2].includes('Alta inicial') && !deA[3].includes('Entrada de nuevo lote'));
  await pagina.screenshot({ path: `${CARPETA}/hu15-2-medicamento.png`, fullPage: true });

  await pagina.selectOption('#filtro-tipo', 'SALIDA');
  const soloSalida = await aplicar(pagina);
  revisar('12/13. Tipo y medicamento combinados muestran solo la salida', soloSalida.length === 1 && soloSalida[0].includes('SALIDA'));

  await pagina.fill('#filtro-desde', '2099-12-31');
  await pagina.fill('#filtro-hasta', '2099-01-01');
  await pagina.getByRole('button', { name: 'Aplicar filtros' }).click();
  revisar('9a. Un rango inválido muestra el mensaje', await aparece(pagina, 'El rango de fechas no es válido.'));
  revisar('9b. Un rango inválido no modifica la lista actual', (await filas(pagina)).length === 1);

  await pagina.fill('#filtro-desde', '2099-01-01');
  await pagina.fill('#filtro-hasta', '2099-12-31');
  const futuro = await aplicar(pagina);
  revisar('14. Sin resultados muestra un mensaje claro', futuro.length === 0 && (await aparece(pagina, 'No hay movimientos que coincidan con los filtros.')));
  await pagina.screenshot({ path: `${CARPETA}/hu15-3-sin-resultados.png`, fullPage: true });

  await pagina.getByRole('button', { name: 'Limpiar filtros' }).click();
  const despuesDeLimpiar = await filas(pagina);
  revisar('15. Limpiar filtros vuelve a mostrar el historial completo', despuesDeLimpiar.length >= 5);
  revisar('15b. Los filtros quedan vacíos', (await pagina.inputValue('#filtro-tipo')) === '' && (await pagina.inputValue('#filtro-desde')) === '');

  await pagina.selectOption('#filtro-medicamento', { label: `${NOMBRE_B} · Tabletas · 500 mg` });
  await pagina.selectOption('#filtro-tipo', 'BAJA');
  const sinBajas = await aplicar(pagina);
  revisar('14b. Un medicamento sin bajas no devuelve movimientos BAJA', sinBajas.length === 0);

  revisar('16. La pantalla no ofrece editar ni borrar movimientos',
    (await pagina.getByRole('button', { name: /editar|eliminar|borrar/i }).count()) === 0);

  await contexto.close();
}

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.route('**/api/bitacora/movimientos**', (ruta) => ruta.abort());
  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);
  await pagina.goto(`${URL}/bitacora`);
  revisar('22a. Si la carga falla se muestra una alerta', await aparece(pagina, 'No se pudo cargar la bitácora'));
  await pagina.unroute('**/api/bitacora/movimientos**');
  await pagina.getByRole('button', { name: 'Intentar de nuevo' }).click();
  revisar('22b. El usuario puede intentarlo de nuevo y se carga la lista', (await filas(pagina)).length >= 5);
  await contexto.close();
}

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, EMPLEADO, 'Farmacia#2026');
  const opcion = await pagina.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Bitácora', exact: true }).count();
  revisar('Solo Administrador: el Empleado no ve "Bitácora" en el menú', opcion === 0);
  await pagina.goto(`${URL}/bitacora`);
  await pagina.waitForURL('**/inicio', { timeout: 10000 }).catch(() => {});
  revisar('Solo Administrador: el Empleado que escribe /bitacora regresa al inicio', pagina.url().includes('/inicio'));
  revisar('Solo Administrador: el Empleado no ve la tabla', !(await pagina.locator('table.tabla').isVisible()));
  await contexto.close();
}

{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/bitacora`);
  await pagina.waitForURL('**/iniciar-sesion', { timeout: 10000 }).catch(() => {});
  revisar('21. Sin sesión, la bitácora regresa al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  await contexto.close();
}

await navegador.close();

revisar('16b. Consultar la bitácora no genera movimientos nuevos', consultar('SELECT COUNT(*) FROM movimiento') === movimientosAntes);

console.log(fallas === 0 ? '\nTodas las comprobaciones de HU-15 pasaron.' : `\n${fallas} comprobación(es) de HU-15 fallaron.`);
process.exit(fallas === 0 ? 0 : 1);
