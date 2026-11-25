// =====================================================================
// Prueba en navegador de HU-06: registro de medicamentos (GitHub Actions con Playwright).
// Requiere: API en :5080, Angular en :4200 y MySQL con la base etbotfarma (la de PRUEBAS de GitHub).
// Solo usa datos inventados (terminación única en cada ejecución) y guarda capturas en e2e/capturas/.
// =====================================================================
import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const CONTRASENIA_ADMIN = process.env.CONTRASENIA ?? 'Prueba123!';
const MYSQL = process.env.MYSQL ?? 'mysql -h127.0.0.1 -uroot -proot -N etbotfarma';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

const SUFIJO = Date.now().toString(36);

/** Fecha yyyy-MM-dd dentro de N meses. */
function enMeses(meses) {
  const fecha = new Date();
  fecha.setMonth(fecha.getMonth() + meses);
  return fecha.toISOString().slice(0, 10);
}

/** Datos de prueba controlados: nadie real, distintos en cada ejecución. */
function medicamento(etiqueta, extra = {}) {
  return {
    nombreComercial: `E2E ${etiqueta} ${SUFIJO}`,
    nombreSustancia: 'Sustancia de prueba',
    presentacion: 'Tabletas',
    concentracion: '500 mg',
    numeroLote: `E2E-${SUFIJO}`,
    fechaCaducidad: enMeses(24),
    cantidadInicial: '100',
    ...extra,
  };
}

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

/** Consulta a MySQL de pruebas y devuelve el texto de la primera columna. */
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

async function llenar(pagina, datos) {
  await pagina.fill('#nombreComercial', datos.nombreComercial);
  await pagina.fill('#nombreSustancia', datos.nombreSustancia);
  await pagina.fill('#presentacion', datos.presentacion);
  await pagina.fill('#concentracion', datos.concentracion);
  await pagina.fill('#numeroLote', datos.numeroLote);
  await pagina.fill('#fechaCaducidad', datos.fechaCaducidad);
  await pagina.fill('#cantidadInicial', datos.cantidadInicial);
}

const navegador = await chromium.launch();
const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
const pagina = await contexto.newPage();
// Si el navegador pregunta "¿Salir de la página?" (datos sin guardar), se acepta
pagina.on('dialog', (dialogo) => dialogo.accept());

try {
  // -------------------------------------------------------------------
  // 1. Entrar como administrador y abrir el formulario desde el menú
  // -------------------------------------------------------------------
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', 'admin');
  await pagina.fill('#contrasenia', CONTRASENIA_ADMIN);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });

  await pagina
    .getByRole('navigation', { name: 'Menú principal' })
    .getByRole('link', { name: /Registrar medicamento/ })
    .click();
  await pagina.waitForURL('**/medicamentos/registrar');
  revisar('El menú lleva al formulario "Registrar medicamento"', await aparece(pagina, 'Datos del medicamento'));
  await pagina.screenshot({ path: `${CARPETA}/hu06-1-formulario.png` });

  // -------------------------------------------------------------------
  // 2. Campos vacíos: se marcan como obligatorios y no se abre la confirmación
  // -------------------------------------------------------------------
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  revisar('Campos vacíos muestran "Este campo es obligatorio"', await aparece(pagina, 'Este campo es obligatorio'));
  revisar('Con errores no se abre la confirmación', (await pagina.getByRole('dialog').count()) === 0);

  // -------------------------------------------------------------------
  // 3. Formatos incorrectos: lote con caracteres no permitidos
  // -------------------------------------------------------------------
  await llenar(pagina, medicamento('formato', { numeroLote: 'LOTE #1' }));
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  revisar(
    'Lote con caracteres no permitidos muestra su mensaje',
    await aparece(pagina, 'El número de lote solo puede tener letras, números y guiones'),
  );
  revisar('Con formato incorrecto no se abre la confirmación', (await pagina.getByRole('dialog').count()) === 0);

  // -------------------------------------------------------------------
  // 4. Cancelar la confirmación conserva los datos
  // -------------------------------------------------------------------
  const normal = medicamento('normal');
  await llenar(pagina, normal);
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  const dialogo = pagina.getByRole('dialog');
  await dialogo.waitFor({ timeout: 5000 });
  const resumen = await dialogo.innerText();
  revisar('La confirmación muestra el resumen del medicamento', resumen.includes(normal.nombreComercial) && resumen.includes(normal.numeroLote));
  revisar('Lote que caduca en 2 años no lleva aviso de caducidad próxima', !resumen.includes('Aviso de caducidad próxima'));
  await pagina.screenshot({ path: `${CARPETA}/hu06-2-confirmacion.png` });
  await dialogo.getByRole('button', { name: 'Cancelar' }).click();
  await dialogo.waitFor({ state: 'hidden', timeout: 5000 });
  revisar('Cancelar conserva lo capturado', (await pagina.inputValue('#nombreComercial')) === normal.nombreComercial);
  revisar(
    'Cancelar no guardó nada en MySQL',
    consultar(`SELECT COUNT(*) FROM medicamento WHERE nombre_comercial = '${normal.nombreComercial}'`) === '0',
  );

  // -------------------------------------------------------------------
  // 5. Aceptar: guarda el medicamento, el lote y la entrada en la bitácora
  // -------------------------------------------------------------------
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  await dialogo.waitFor({ timeout: 5000 });
  await dialogo.getByRole('button', { name: 'Aceptar' }).click();
  revisar('Muestra "Medicamento registrado"', await aparece(pagina, 'Medicamento registrado', 10000));
  await pagina.screenshot({ path: `${CARPETA}/hu06-3-registrado.png` });
  const idNormal = consultar(`SELECT id_medicamento FROM medicamento WHERE nombre_comercial = '${normal.nombreComercial}'`);
  revisar('El medicamento quedó guardado en MySQL', idNormal !== '');
  revisar(
    'El lote quedó con 100 unidades',
    consultar(`SELECT stock FROM lote WHERE id_medicamento = ${idNormal || 0} AND numero_lote = '${normal.numeroLote}'`) === '100',
  );
  revisar(
    'Quedó una ENTRADA de 100 unidades en la bitácora',
    consultar(`SELECT COUNT(*) FROM movimiento WHERE id_medicamento = ${idNormal || 0} AND tipo = 'ENTRADA' AND cantidad_total = 100`) === '1',
  );
  revisar('El formulario quedó limpio para el siguiente registro', (await pagina.inputValue('#nombreComercial')) === '');

  // -------------------------------------------------------------------
  // 6. Cantidad 0: se guarda el medicamento y su lote, pero no hay entrada
  // -------------------------------------------------------------------
  const sinStock = medicamento('cero', { cantidadInicial: '0' });
  await llenar(pagina, sinStock);
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  await dialogo.waitFor({ timeout: 5000 });
  // El resumen debe mostrar el medicamento NUEVO, no el del registro anterior
  revisar('La confirmación del segundo registro muestra los datos nuevos', (await dialogo.innerText()).includes(sinStock.nombreComercial));
  await dialogo.getByRole('button', { name: 'Aceptar' }).click();
  await dialogo.waitFor({ state: 'hidden', timeout: 10000 });
  const idCero = consultar(`SELECT id_medicamento FROM medicamento WHERE nombre_comercial = '${sinStock.nombreComercial}'`);
  revisar('Con cantidad 0 el medicamento sí se guarda', idCero !== '');
  revisar(
    'Con cantidad 0 no hay movimiento en la bitácora',
    consultar(`SELECT COUNT(*) FROM movimiento WHERE id_medicamento = ${idCero || 0}`) === '0',
  );

  // -------------------------------------------------------------------
  // 7. Lote que caduca en 2 meses: la confirmación avisa que quedará en Rojo
  // -------------------------------------------------------------------
  await llenar(pagina, medicamento('rojo', { fechaCaducidad: enMeses(2) }));
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  await dialogo.waitFor({ timeout: 5000 });
  revisar('Caducidad en 2 meses muestra el aviso de caducidad próxima', (await dialogo.innerText()).includes('Aviso de caducidad próxima'));
  await dialogo.getByRole('button', { name: 'Cancelar' }).click();
  await dialogo.waitFor({ state: 'hidden', timeout: 5000 });

  // -------------------------------------------------------------------
  // 8. Medicamento repetido: el servidor lo rechaza y se conservan los datos
  // -------------------------------------------------------------------
  await pagina.reload();
  await llenar(pagina, { ...normal, numeroLote: `E2E-OTRO-${SUFIJO}` });
  await pagina.getByRole('button', { name: 'Guardar', exact: true }).click();
  await dialogo.waitFor({ timeout: 5000 });
  await dialogo.getByRole('button', { name: 'Aceptar' }).click();
  revisar('Medicamento repetido muestra "ya está registrado"', await aparece(pagina, 'El medicamento ya está registrado', 10000));
  revisar('Tras el rechazo se conservan los datos capturados', (await pagina.inputValue('#nombreComercial')) === normal.nombreComercial);
  revisar(
    'El repetido no creó otro medicamento',
    consultar(`SELECT COUNT(*) FROM medicamento WHERE nombre_comercial = '${normal.nombreComercial}'`) === '1',
  );
  await pagina.screenshot({ path: `${CARPETA}/hu06-4-repetido.png` });
} catch (error) {
  console.log(`FALLA  La prueba se detuvo con un error: ${error.message}`);
  await pagina.screenshot({ path: `${CARPETA}/hu06-error.png` }).catch(() => {});
  fallas++;
} finally {
  await navegador.close();
}

console.log(fallas === 0 ? '\nHU-06 en navegador: todo OK' : `\nHU-06 en navegador: ${fallas} falla(s)`);
process.exit(fallas === 0 ? 0 : 1);
