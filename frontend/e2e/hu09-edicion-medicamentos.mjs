// =====================================================================
// Prueba en navegador de HU-09: edición de información de medicamentos (GitHub Actions con Playwright).
// Requiere: API en :5080, Angular en :4200 y MySQL con la base etbotfarma (la de PRUEBAS de GitHub).
// Solo usa datos inventados (terminación única) y NUNCA cambia medicamentos de otras pruebas.
// Guarda capturas en e2e/capturas/.
// =====================================================================
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
const NOMBRE = `E2E Edicion ${SUFIJO}`;
const NOMBRE_DUP = `E2E Edicion Dup ${SUFIJO}`;
const LUIS = `e2e.edicion.${SUFIJO}`;   // un Empleado (escenario 3)

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

// ---------------------------------------------------------------------
// Datos de prueba: dos medicamentos (uno con lote, para el escenario 11) y un Empleado
// ---------------------------------------------------------------------
const idMedicamento = consultar(
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE}', 'Sustancia E2E', 'Tabletas', '500 mg', 0, 0, 1, NOW()); SELECT LAST_INSERT_ID();`,
);
consultar(
  `INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (${idMedicamento}, 'LOTE-E2E-${SUFIJO}', DATE_ADD(CURDATE(), INTERVAL 24 MONTH), 40, CURDATE());`,
);
consultar(
  `INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, activo, fecha_registro) VALUES ('${NOMBRE_DUP}', 'Sustancia E2E Dup', 'Jarabe', '250 mg', 0, 0, 1, NOW());`,
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
    telefono: '5500000009', contrasenia: 'Farmacia#2026', idRol: 2,
  }),
});

const stockDeLote = () => consultar(`SELECT stock FROM lote WHERE id_medicamento = ${idMedicamento}`);
const datosDe = (id) => consultar(
  `SELECT CONCAT(nombre_comercial, '|', presentacion, '|', concentracion, '|', es_controlado) FROM medicamento WHERE id_medicamento = ${id}`,
);
const auditoriasDe = (id) => consultar(`SELECT COUNT(*) FROM auditoria WHERE accion = 'EDITAR MEDICAMENTO' AND id_registro = ${id}`);

const navegador = await chromium.launch();

// ---------------------------------------------------------------------
// Escenario 4: sin sesión, la dirección directa manda al inicio de sesión
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/medicamentos/${idMedicamento}/editar`);
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 10000 }).catch(() => {});
  revisar('4. Sin sesión, la dirección directa manda al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  await contexto.close();
}

// ---------------------------------------------------------------------
// Escenario 3: un Empleado no ve "Editar" en el inventario ni puede entrar escribiendo la dirección
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', LUIS);
  await pagina.fill('#contrasenia', 'Farmacia#2026');
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });

  await pagina.goto(`${URL}/inventario`);
  await pagina.getByLabel('Buscar medicamento').fill(NOMBRE);
  await pagina.waitForTimeout(300);
  revisar('3a. El Empleado no ve el botón "Editar" en el inventario', (await pagina.getByRole('link', { name: 'Editar' }).count()) === 0);

  await pagina.goto(`${URL}/medicamentos/${idMedicamento}/editar`);
  await pagina.waitForURL('**/inicio', { timeout: 10000 }).catch(() => {});
  revisar('3b. El Empleado que escribe la dirección regresa a su inicio', pagina.url().includes('/inicio'));
  await contexto.close();
}

// ---------------------------------------------------------------------
// Administrador: escenarios 1, 2, 5 a 16
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', 'admin');
  await pagina.fill('#contrasenia', CONTRASENIA_ADMIN);
  await pagina.click('button[type=submit]');
  await pagina.waitForURL('**/inicio', { timeout: 15000 });

  // El medicamento se elige desde el inventario (HU-08)
  await pagina.goto(`${URL}/inventario`);
  await pagina.getByLabel('Buscar medicamento').fill(NOMBRE);
  await pagina.getByRole('link', { name: 'Editar' }).first().click();
  await pagina.waitForURL(`**/medicamentos/${idMedicamento}/editar`);

  const dialogo = pagina.getByRole('dialog', { name: '¿Guardar los cambios de este medicamento?' });
  const guardar = () => pagina.getByRole('button', { name: 'Guardar' });

  // Escenario 2: formulario precargado con los valores actuales
  revisar('2a. Precarga el nombre comercial', (await pagina.inputValue('#nombreComercial')) === NOMBRE);
  revisar('2b. Precarga la presentación', (await pagina.inputValue('#presentacion')) === 'Tabletas');
  revisar('2c. El checkbox de controlado inicia sin marcar', !(await pagina.isChecked('#esControlado')));

  // Escenario 10: existencias de referencia, bloqueadas (no hay campos para editarlas)
  revisar('10a. Muestra las existencias totales como referencia', await aparece(pagina, '40 unidades'));
  revisar('10b. No hay ningún campo para stock, lote o fecha de caducidad', (await pagina.locator('input[formcontrolname*="lote" i], input[formcontrolname*="stock" i], input[formcontrolname*="caducidad" i]').count()) === 0);
  await pagina.screenshot({ path: `${CARPETA}/hu09-1-formulario.png`, fullPage: true });

  // Escenario 12: sin cambios, "Guardar" permanece deshabilitado
  revisar('12. Sin cambios, el botón "Guardar" está deshabilitado', await guardar().isDisabled());

  // Escenario 7: borrar un campo obligatorio no abre la confirmación
  await pagina.fill('#nombreComercial', '');
  await pagina.waitForTimeout(300); // deja correr la detección de cambios de Angular antes de revisar el botón
  revisar('7a. Al vaciar un campo "Guardar" se habilita (hay un cambio)', !(await guardar().isDisabled()));
  await guardar().click();
  revisar('7b. Marca "Este campo es obligatorio"', await aparece(pagina, 'Este campo es obligatorio'));
  revisar('7c. No se abre la confirmación', !(await dialogo.isVisible()));

  // Deja los datos listos para el resto de la prueba
  await pagina.fill('#nombreComercial', `${NOMBRE} Forte`);
  await pagina.fill('#presentacion', 'Jarabe');
  await pagina.fill('#concentracion', '250 mg/5 ml');
  await pagina.check('#esControlado');

  // Escenarios 5 y 6: confirmación con resumen antes/después; "Cancelar" no cambia nada
  await guardar().click();
  await dialogo.waitFor({ timeout: 5000 });
  const textoDialogo = await dialogo.innerText();
  revisar('5a. El resumen muestra el valor ANTERIOR', textoDialogo.includes('Tabletas') && textoDialogo.includes('500 mg'));
  revisar('5b. El resumen muestra el valor NUEVO', textoDialogo.includes('Jarabe') && textoDialogo.includes('250 mg/5 ml'));
  await pagina.screenshot({ path: `${CARPETA}/hu09-2-confirmacion.png`, fullPage: true });
  await dialogo.getByRole('button', { name: 'Cancelar' }).click();
  await pagina.waitForTimeout(300);
  revisar('6a. Al cancelar sigue en el formulario con lo capturado', (await pagina.inputValue('#presentacion')) === 'Jarabe');
  revisar('6b. Al cancelar nada cambió en la base', datosDe(idMedicamento).startsWith(`${NOMBRE}|Tabletas|500 mg|0`));

  // Escenario 15: el servidor no responde
  await pagina.route(`**/api/medicamentos/${idMedicamento}`, (ruta) => (ruta.request().method() === 'PUT' ? ruta.abort('internetdisconnected') : ruta.continue()));
  await guardar().click();
  await dialogo.getByRole('button', { name: 'Aceptar' }).click();
  revisar('15a. Si falla la conexión muestra una alerta de error', await aparece(pagina, 'No se pudo actualizar el medicamento'));
  revisar('15b. No queda ningún cambio a medias', datosDe(idMedicamento).startsWith(`${NOMBRE}|Tabletas|500 mg|0`) && auditoriasDe(idMedicamento) === '0');
  await pagina.unroute(`**/api/medicamentos/${idMedicamento}`);

  // Escenarios 1, 11 y 16: "Aceptar" varias veces → una sola actualización
  let peticiones = 0;
  pagina.on('request', (peticion) => {
    if (peticion.method() === 'PUT' && peticion.url().endsWith(`/api/medicamentos/${idMedicamento}`)) peticiones++;
  });
  await guardar().click();
  await dialogo.getByRole('button', { name: 'Aceptar' }).evaluate((boton) => { boton.click(); boton.click(); boton.click(); });
  revisar('1a. Muestra "Medicamento actualizado"', await aparece(pagina, 'Medicamento actualizado', 8000));
  revisar('16. Varios clics en "Aceptar" envían UNA sola petición', peticiones === 1);
  revisar('1b. En la base quedó el nombre, presentación y concentración nuevos, y "controlado" activo',
    datosDe(idMedicamento) === `${NOMBRE} Forte|Jarabe|250 mg/5 ml|1`);
  revisar('11. El lote y su stock no se tocaron', stockDeLote() === '40');
  await pagina.screenshot({ path: `${CARPETA}/hu09-3-actualizado.png`, fullPage: true });

  // Escenario 1 (auditoría): quién, qué tabla, qué registro
  const auditoria = consultar(
    `SELECT CONCAT(quien.nombre_usuario, '|', a.tabla_afectada, '|', IFNULL(a.valor_anterior, ''), '|', IFNULL(a.valor_nuevo, '')) FROM auditoria a JOIN usuario quien ON quien.id_usuario = a.id_usuario WHERE a.accion = 'EDITAR MEDICAMENTO' AND a.id_registro = ${idMedicamento} ORDER BY a.id_auditoria DESC LIMIT 1`,
  );
  revisar('1c. La auditoría registra quién (admin) y la tabla "medicamento"', auditoria.startsWith('admin|medicamento|'));
  revisar('1d. La auditoría guarda el antes y el después (sin stock ni lotes)', auditoria.includes('Tabletas') && auditoria.includes('Jarabe') && !auditoria.toLowerCase().includes('lote'));

  // Escenario 9: cambiar SOLO "Medicamento controlado" del mismo registro se guarda sin chocar
  await pagina.uncheck('#esControlado');
  await guardar().click();
  await dialogo.getByRole('button', { name: 'Aceptar' }).click();
  revisar('9. Cambiar solo "Medicamento controlado" se guarda sin error de duplicado', await aparece(pagina, 'Medicamento actualizado', 8000));
  revisar('9b. El valor quedó desmarcado en la base', datosDe(idMedicamento).endsWith('|0'));

  // Escenario 12 (otra vez): tras guardar, sin tocar nada "Guardar" vuelve a estar deshabilitado
  revisar('12b. Después de guardar, sin cambios nuevos "Guardar" está deshabilitado', await guardar().isDisabled());

  // Escenario 8: la combinación ya existe en OTRO medicamento → 422, nada cambia
  await pagina.fill('#nombreComercial', NOMBRE_DUP);
  await pagina.fill('#nombreSustancia', 'Sustancia E2E Dup');
  await pagina.fill('#presentacion', 'Jarabe');
  await pagina.fill('#concentracion', '250 mg');
  await guardar().click();
  await dialogo.getByRole('button', { name: 'Aceptar' }).click();
  revisar('8a. Muestra la alerta de duplicado', await aparece(pagina, 'Ya existe otro medicamento registrado con esos datos'));
  revisar('8b. Nada cambió en la base', datosDe(idMedicamento) === `${NOMBRE} Forte|Jarabe|250 mg/5 ml|0`);

  // Escenario 13: cambios sin guardar → advertencia al salir (Cancelar se queda, Aceptar sale)
  await pagina.fill('#nombreComercial', `${NOMBRE} Forte`); // deja el campo válido otra vez, pero DISTINTO (hay cambio pendiente)
  await pagina.fill('#presentacion', 'Cambio sin guardar');
  pagina.once('dialog', (d) => d.dismiss());
  await pagina.getByRole('link', { name: 'Volver al inventario' }).click();
  await pagina.waitForTimeout(500);
  revisar('13a. Al cancelar la advertencia, sigue en el formulario', pagina.url().includes('/editar'));

  pagina.once('dialog', (d) => d.accept());
  await pagina.getByRole('link', { name: 'Volver al inventario' }).click();
  await pagina.waitForURL('**/inventario', { timeout: 10000 });
  revisar('13b. Al aceptar la advertencia, sale del formulario', pagina.url().includes('/inventario'));
  revisar('13c. Al salir sin guardar, los datos anteriores quedaron intactos', datosDe(idMedicamento) === `${NOMBRE} Forte|Jarabe|250 mg/5 ml|0`);

  // Escenario 14: sin ningún cambio, salir no muestra ninguna advertencia
  await pagina.getByLabel('Buscar medicamento').fill(NOMBRE);
  await pagina.getByRole('link', { name: 'Editar' }).first().click();
  await pagina.waitForURL(`**/medicamentos/${idMedicamento}/editar`);
  let huboDialogo = false;
  pagina.once('dialog', () => { huboDialogo = true; });
  await pagina.getByRole('link', { name: 'Volver al inventario' }).click();
  await pagina.waitForURL('**/inventario', { timeout: 10000 });
  revisar('14. Sin cambios, salir no muestra ninguna advertencia', !huboDialogo);

  // Un id que no existe (dirección escrita a mano)
  await pagina.goto(`${URL}/medicamentos/999999/editar`);
  revisar('404. Un id que no existe muestra "El medicamento no existe"', await aparece(pagina, 'El medicamento no existe'));
  revisar('404b. Y no se muestra el formulario', !(await pagina.locator('#nombreComercial').count()));

  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-09 en navegador: todas las pruebas pasaron' : `HU-09 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
