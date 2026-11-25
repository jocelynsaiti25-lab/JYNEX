import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const CONTRASENIA_ADMIN = process.env.CONTRASENIA ?? 'Prueba123!';
const MYSQL = process.env.MYSQL ?? 'mysql -h127.0.0.1 -uroot -proot -N etbotfarma';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

const SUFIJO = Date.now().toString(36);
const NOMBRE = `E2E Entrada ${SUFIJO}`;
const LOTE = `E2E-L-${SUFIJO}`;

function enMeses(meses) {
  const fecha = new Date();
  fecha.setMonth(fecha.getMonth() + meses);
  return fecha.toISOString().slice(0, 10);
}

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
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE}', 'Sustancia de prueba', 'Tabletas', '500 mg', 0, 10, 1, NOW()); SELECT LAST_INSERT_ID();`,
);

async function llenarLote(pagina, numeroLote, fecha, cantidad) {
  await pagina.fill('#numeroLote', numeroLote);
  await pagina.fill('#fechaCaducidad', fecha);
  await pagina.fill('#cantidad', cantidad);
}

const navegador = await chromium.launch();
const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
const pagina = await contexto.newPage();
pagina.on('dialog', (dialogo) => dialogo.accept());

try {
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', 'admin');
  await pagina.fill('#contrasenia', CONTRASENIA_ADMIN);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });

  await pagina
    .getByRole('navigation', { name: 'Menú principal' })
    .getByRole('link', { name: /Entrada de lote/ })
    .click();
  await pagina.waitForURL('**/inventario/entrada-lote');
  revisar('El menú lleva a "Registro de entrada de lote"', await aparece(pagina, 'Selecciona un medicamento activo'));
  revisar('Sin medicamento elegido pide elegir uno primero', await aparece(pagina, 'Elige primero un medicamento activo'));

  await pagina.fill('input[placeholder^="Buscar por nombre"]', NOMBRE);
  const fila = pagina.locator('ul.lista-medicamentos li', { hasText: NOMBRE });
  await fila.waitFor({ timeout: 5000 });
  revisar('La búsqueda encuentra el medicamento activo', true);
  revisar('Muestra "Agotado" porque no tiene lotes con unidades', (await fila.innerText()).includes('Agotado'));
  await fila.getByRole('button', { name: 'Elegir' }).click();
  revisar('Al elegirlo aparece el formulario del lote', await aparece(pagina, 'Datos del nuevo lote'));
  await pagina.screenshot({ path: `${CARPETA}/hu07-1-formulario.png` });

  await pagina.fill('#numeroLote', '');
  await pagina.locator('#numeroLote').blur();
  revisar('Lote vacío muestra "El número de lote es obligatorio."', await aparece(pagina, 'El número de lote es obligatorio.'));

  await pagina.fill('#fechaCaducidad', enMeses(2));
  revisar('Caducidad en 2 meses avisa semáforo rojo', await aparece(pagina, 'Este lote vence en 3 meses o menos'));
  await pagina.fill('#fechaCaducidad', enMeses(-1));
  revisar('Fecha vencida muestra que no puede registrarse', await aparece(pagina, 'no puede ser hoy ni una fecha vencida'));

  await llenarLote(pagina, LOTE, enMeses(24), '5');
  const dialogo = pagina.getByRole('dialog');
  await pagina.getByRole('button', { name: 'Revisar y confirmar entrada' }).click();
  await dialogo.waitFor({ timeout: 5000 });
  const resumen = await dialogo.innerText();
  revisar('El resumen muestra medicamento, lote y cantidad', resumen.includes(NOMBRE) && resumen.includes(LOTE) && resumen.includes('+5 unidades'));
  revisar('El resumen calcula 5 unidades resultantes', resumen.includes('5 unidades'));
  await pagina.screenshot({ path: `${CARPETA}/hu07-2-confirmacion.png` });

  await dialogo.getByRole('button', { name: 'Cancelar y editar' }).click();
  await dialogo.waitFor({ state: 'hidden', timeout: 5000 });
  revisar('Cancelar conserva lo capturado', (await pagina.inputValue('#numeroLote')) === LOTE);
  revisar(
    'Cancelar no guardó nada en MySQL',
    consultar(`SELECT COUNT(*) FROM lote WHERE id_medicamento = ${idMedicamento}`) === '0',
  );

  await pagina.getByRole('button', { name: 'Revisar y confirmar entrada' }).click();
  await dialogo.waitFor({ timeout: 5000 });
  await dialogo.getByRole('button', { name: 'Confirmar entrada' }).click();
  revisar('Muestra "Entrada de lote registrada correctamente."', await aparece(pagina, 'Entrada de lote registrada correctamente.', 10000));
  revisar('Muestra las existencias actualizadas', await aparece(pagina, 'Existencias totales actualizadas'));
  await pagina.screenshot({ path: `${CARPETA}/hu07-3-registrada.png` });
  revisar(
    'El lote quedó con 5 unidades en MySQL',
    consultar(`SELECT stock FROM lote WHERE id_medicamento = ${idMedicamento} AND numero_lote = '${LOTE}'`) === '5',
  );
  revisar(
    'Quedó una ENTRADA de 5 unidades en la bitácora con su detalle',
    consultar(
      `SELECT COUNT(*) FROM movimiento m INNER JOIN detalle_movimiento d ON d.id_movimiento = m.id_movimiento WHERE m.id_medicamento = ${idMedicamento} AND m.tipo = 'ENTRADA' AND m.cantidad_total = 5 AND d.cantidad = 5`,
    ) === '1',
  );
  revisar('Con 5 unidades y mínimo 10 la lista marca "Stock bajo"', await aparece(pagina, 'Stock bajo'));

  await llenarLote(pagina, LOTE, enMeses(24), '3');
  await pagina.getByRole('button', { name: 'Revisar y confirmar entrada' }).click();
  await dialogo.waitFor({ timeout: 5000 });
  revisar('El segundo resumen muestra los datos nuevos', (await dialogo.innerText()).includes('+3 unidades'));
  await dialogo.getByRole('button', { name: 'Confirmar entrada' }).click();
  revisar('Lote repetido muestra "ya existe para este medicamento"', await aparece(pagina, 'ya existe para este medicamento', 10000));
  revisar(
    'El repetido no creó otro lote',
    consultar(`SELECT COUNT(*) FROM lote WHERE id_medicamento = ${idMedicamento}`) === '1',
  );
  await pagina.screenshot({ path: `${CARPETA}/hu07-4-repetido.png` });
} catch (error) {
  console.log(`FALLA  La prueba se detuvo con un error: ${error.message}`);
  await pagina.screenshot({ path: `${CARPETA}/hu07-error.png` }).catch(() => {});
  fallas++;
} finally {
  await navegador.close();
}

console.log(fallas === 0 ? '\nHU-07 en navegador: todo OK' : `\nHU-07 en navegador: ${fallas} falla(s)`);
process.exit(fallas === 0 ? 0 : 1);
