// =====================================================================
// Prueba en navegador de HU-05: reasignación de contraseña (GitHub Actions con Playwright).
// Requiere: API en :5080, Angular en :4200 y MySQL con la base etbotfarma (la de PRUEBAS de GitHub).
// Solo usa datos inventados (terminación única) y NUNCA cambia la contraseña del "admin".
// Guarda capturas en e2e/capturas/.
// =====================================================================
import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const API = process.env.API ?? 'http://localhost:5080';
const CONTRASENIA_ADMIN = process.env.CONTRASENIA ?? 'Prueba123!';
// utf8mb4: sin esto el cliente mysql usa latin1 y no encuentra 'REASIGNAR CONTRASEÑA' (por la Ñ)
const MYSQL = process.env.MYSQL ?? 'mysql --default-character-set=utf8mb4 -h127.0.0.1 -uroot -proot -N etbotfarma';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

const SUFIJO = Date.now().toString(36);
const VIEJA = 'Farmacia2026';
const NUEVA = 'Reasignada2027';
const ANA = `e2e.ana.${SUFIJO}`;            // la persona que perdió el acceso
const LUIS = `e2e.luis.${SUFIJO}`;          // un Empleado (escenario 5)
const INACTIVA = `e2e.inactiva.${SUFIJO}`;  // escenario 8
const ADMIN2 = `e2e.admin2.${SUFIJO}`;      // escenario 17: un Administrador que cambia la suya

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

function consultar(sql) {
  return execSync(`${MYSQL} -e "${sql}"`).toString().trim();
}

const idDe = (usuario) => consultar(`SELECT id_usuario FROM usuario WHERE nombre_usuario = '${usuario}'`);
const hashDe = (usuario) => consultar(`SELECT contrasenia_hash FROM usuario WHERE nombre_usuario = '${usuario}'`);

async function crearCuentas() {
  const login = await fetch(`${API}/api/sesiones/iniciar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario: 'admin', contrasenia: CONTRASENIA_ADMIN }),
  });
  const cookie = login.headers.getSetCookie()[0].split(';')[0];
  const crear = async (nombreUsuario, nombre, idRol) => {
    const respuesta = await fetch(`${API}/api/usuarios`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Cookie: cookie },
      body: JSON.stringify({
        nombre, apellidos: 'Prueba E2E', nombreUsuario, correo: `${nombreUsuario}@etbotfarma.test`,
        telefono: '5500000005', contrasenia: VIEJA, idRol,
      }),
    });
    if (respuesta.status !== 201) throw new Error(`No se pudo crear ${nombreUsuario}: ${respuesta.status}`);
  };
  await crear(ANA, 'Ana', 2);
  await crear(LUIS, 'Luis', 2);
  await crear(INACTIVA, 'Inés', 2);
  await crear(ADMIN2, 'Alma', 1);
  await fetch(`${API}/api/sesiones/cerrar`, { method: 'POST', headers: { Cookie: cookie } });
  consultar(`UPDATE usuario SET activo = FALSE WHERE nombre_usuario = '${INACTIVA}'`);   // aún no existe la historia de desactivar
}

async function intentarIniciar(pagina, nombreUsuario, contrasenia) {
  await pagina.goto(`${URL}/iniciar-sesion`);
  await pagina.fill('#nombreUsuario', nombreUsuario);
  await pagina.fill('#contrasenia', contrasenia);
  await pagina.click('button[type=submit]');
  return pagina.waitForURL('**/inicio', { timeout: 8000 }).then(() => true, () => false);
}

async function iniciarSesion(pagina, nombreUsuario, contrasenia) {
  if (!(await intentarIniciar(pagina, nombreUsuario, contrasenia))) throw new Error(`No pudo entrar ${nombreUsuario}`);
}

async function aparece(pagina, texto, tiempo = 5000) {
  try {
    await pagina.getByText(texto).first().waitFor({ timeout: tiempo });
    return true;
  } catch {
    return false;
  }
}

async function llenar(pagina, nueva, confirmar) {
  await pagina.fill('#nuevaContrasenia', nueva);
  await pagina.fill('#confirmarContrasenia', confirmar);
}

const dialogo = (pagina) => pagina.getByRole('dialog', { name: '¿Cambiar la contraseña de esta cuenta?' });
const dialogoAbierto = (pagina) => dialogo(pagina).isVisible();

await crearCuentas();
const navegador = await chromium.launch();

// ---------------------------------------------------------------------
// La persona (Ana) tiene una sesión abierta en otro equipo (escenario 12)
// ---------------------------------------------------------------------
const otroEquipo = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
const paginaAna = await otroEquipo.newPage();
await iniciarSesion(paginaAna, ANA, VIEJA);

// ---------------------------------------------------------------------
// Administrador: escenarios 1 a 4, 7 a 11, 13 a 16
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, 'admin', CONTRASENIA_ADMIN);

  // La cuenta se elige desde la consulta de usuarios (HU-04)
  await pagina.goto(`${URL}/usuarios`);
  await pagina.fill('#busqueda', ANA);
  await pagina.getByRole('link', { name: `Reasignar contraseña de ${ANA}` }).click();
  await pagina.waitForURL(`**/usuarios/${idDe(ANA)}/contrasenia`);
  revisar('0. La cuenta se elige desde la lista de usuarios', await aparece(pagina, `${ANA}@etbotfarma.test`));

  // Escenario 4: solo la nueva contraseña y su confirmación
  const camposContrasenia = await pagina.locator('input[type=password]').count();
  const etiquetas = (await pagina.locator('form label').allInnerTexts()).map((e) => e.replace('*', '').trim());
  revisar('4. Solo se piden "Nueva contraseña" y "Confirmar contraseña"',
    camposContrasenia === 2 && etiquetas.join('|') === 'Nueva contraseña|Confirmar contraseña');
  await pagina.screenshot({ path: `${CARPETA}/hu05-1-formulario.png`, fullPage: true });

  // Escenario 11: campos vacíos
  await pagina.getByRole('button', { name: 'Guardar' }).click();
  await aparece(pagina, 'Este campo es obligatorio');   // espera a que se muestren los mensajes
  revisar('11a. Cada campo vacío dice "Este campo es obligatorio"', (await pagina.getByText('Este campo es obligatorio').count()) === 2);
  revisar('11b. Con campos vacíos no se abre la confirmación', !(await dialogoAbierto(pagina)));

  // Escenario 9: política de seguridad
  await llenar(pagina, 'SinNumeroAqui', 'SinNumeroAqui');
  await pagina.getByRole('button', { name: 'Guardar' }).click();
  revisar('9a. Dice qué requisito falta ("La contraseña debe tener al menos un número.")', await aparece(pagina, 'La contraseña debe tener al menos un número.'));
  revisar('9b. No se abre la confirmación', !(await dialogoAbierto(pagina)));

  // Escenario 10: no coinciden
  await llenar(pagina, NUEVA, `${NUEVA}x`);
  await pagina.getByRole('button', { name: 'Guardar' }).click();
  revisar('10a. Muestra "Las contraseñas no coinciden"', await aparece(pagina, 'Las contraseñas no coinciden'));
  revisar('10b. No se abre la confirmación', !(await dialogoAbierto(pagina)));

  // Escenarios 2 y 3: confirmación y "Cancelar"
  const hashAntes = hashDe(ANA);
  await llenar(pagina, NUEVA, NUEVA);
  await pagina.getByRole('button', { name: 'Guardar' }).click();
  await dialogo(pagina).waitFor({ timeout: 5000 });
  const textoDialogo = await dialogo(pagina).innerText();
  revisar('2a. La confirmación muestra nombre completo, usuario y correo',
    textoDialogo.includes('Ana Prueba E2E') && textoDialogo.includes(ANA) && textoDialogo.includes(`${ANA}@etbotfarma.test`));
  revisar('2b. La confirmación NO muestra la contraseña', !textoDialogo.includes(NUEVA));
  await pagina.screenshot({ path: `${CARPETA}/hu05-2-confirmacion.png`, fullPage: true });
  await dialogo(pagina).getByRole('button', { name: 'Cancelar' }).click();
  await pagina.waitForTimeout(500);
  revisar('3a. Al cancelar sigue en el formulario', pagina.url().includes('/contrasenia') && (await pagina.inputValue('#nuevaContrasenia')) === NUEVA);
  revisar('3b. Al cancelar la contraseña NO cambia', hashDe(ANA) === hashAntes);

  // Escenario 15: el servidor no responde
  await pagina.route('**/api/usuarios/*/contrasenia', (ruta) => ruta.abort('internetdisconnected'));
  await pagina.getByRole('button', { name: 'Guardar' }).click();
  await dialogo(pagina).getByRole('button', { name: 'Aceptar' }).click();
  revisar('15a. Si falla la conexión muestra una alerta de error', await aparece(pagina, 'No se pudo actualizar la contraseña'));
  revisar('15b. No queda ningún cambio a medias', hashDe(ANA) === hashAntes && consultar(`SELECT COUNT(*) FROM auditoria WHERE accion = 'REASIGNAR CONTRASEÑA' AND id_registro = ${idDe(ANA)}`) === '0');
  await pagina.unroute('**/api/usuarios/*/contrasenia');

  // Escenarios 1 y 16: "Aceptar" varias veces → un solo cambio
  let peticiones = 0;
  pagina.on('request', (peticion) => {
    if (peticion.method() === 'PUT' && peticion.url().includes('/contrasenia')) peticiones++;
  });
  await pagina.getByRole('button', { name: 'Guardar' }).click();
  await dialogo(pagina).getByRole('button', { name: 'Aceptar' }).evaluate((boton) => { boton.click(); boton.click(); boton.click(); });
  revisar('1a. Muestra "Contraseña actualizada correctamente"', await aparece(pagina, 'Contraseña actualizada correctamente', 8000));
  revisar('16. Varios clics en "Aceptar" envían UNA sola petición', peticiones === 1);
  revisar('1b. En la base se guardó un hash (no el texto) distinto al anterior', hashDe(ANA) !== hashAntes && !hashDe(ANA).includes(NUEVA));
  revisar('1c. El formulario queda vacío (la contraseña no se queda en pantalla)', (await pagina.inputValue('#nuevaContrasenia')) === '');
  await pagina.screenshot({ path: `${CARPETA}/hu05-3-actualizada.png`, fullPage: true });

  // Escenario 14: auditoría
  const auditoria = consultar(`SELECT CONCAT(quien.nombre_usuario, '|', IFNULL(a.valor_nuevo, ''), '|', IFNULL(a.valor_anterior, '')) FROM auditoria a JOIN usuario quien ON quien.id_usuario = a.id_usuario WHERE a.accion = 'REASIGNAR CONTRASEÑA' AND a.id_registro = ${idDe(ANA)}`);
  revisar('14a. La auditoría registra quién (admin) y a qué cuenta', auditoria.startsWith('admin|') && auditoria.includes(ANA));
  revisar('14b. La auditoría no guarda la contraseña ni su hash', !auditoria.includes(NUEVA) && !auditoria.includes(hashDe(ANA)));

  // Escenario 7: cuenta que no existe (dirección escrita a mano)
  await pagina.goto(`${URL}/usuarios/999999/contrasenia`);
  revisar('7. Una cuenta que no existe muestra "La cuenta no existe"', await aparece(pagina, 'La cuenta no existe'));
  revisar('7b. Y no se muestra el formulario', !(await pagina.locator('#nuevaContrasenia').count()));

  // Escenario 8: cuenta desactivada
  const hashInactiva = hashDe(INACTIVA);
  await pagina.goto(`${URL}/usuarios/${idDe(INACTIVA)}/contrasenia`);
  revisar('8a. Una cuenta desactivada muestra "La cuenta está desactivada"', await aparece(pagina, 'La cuenta está desactivada'));
  revisar('8b. No permite el cambio (sin formulario y sin cambios en la base)', !(await pagina.locator('#nuevaContrasenia').count()) && hashDe(INACTIVA) === hashInactiva);
  await contexto.close();
}

// ---------------------------------------------------------------------
// Escenario 12: la sesión de Ana en el otro equipo ya no sirve
// ---------------------------------------------------------------------
await paginaAna.reload();
await paginaAna.waitForURL('**/iniciar-sesion**', { timeout: 15000 }).catch(() => {});
revisar('12. La sesión anterior de la persona la regresa al inicio de sesión', paginaAna.url().includes('/iniciar-sesion'));

// Escenario 13: con la anterior no entra, con la nueva sí (con su rol)
revisar('13a. Con la contraseña anterior no entra', !(await intentarIniciar(paginaAna, ANA, VIEJA)));
revisar('13b. Con la anterior ve "Usuario o contraseña incorrectos."', await aparece(paginaAna, 'Usuario o contraseña incorrectos.'));
revisar('13c. Con la nueva entra al sistema', await intentarIniciar(paginaAna, ANA, NUEVA));
revisar('13d. Entra con los permisos de su rol (Empleado: sin "Usuarios")',
  (await paginaAna.getByRole('navigation', { name: 'Menú principal' }).getByRole('link', { name: 'Usuarios', exact: true }).count()) === 0);
await otroEquipo.close();

// ---------------------------------------------------------------------
// Escenario 5: un Empleado no puede entrar
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, LUIS, VIEJA);
  await pagina.goto(`${URL}/usuarios/${idDe(ANA)}/contrasenia`);
  await pagina.waitForURL('**/inicio', { timeout: 10000 }).catch(() => {});
  revisar('5a. El Empleado que escribe la dirección regresa a su inicio', pagina.url().includes('/inicio'));
  revisar('5b. El Empleado no ve el formulario', !(await pagina.locator('#nuevaContrasenia').count()));
  await contexto.close();
}

// ---------------------------------------------------------------------
// Escenario 17: un Administrador cambia su propia contraseña
// ---------------------------------------------------------------------
{
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 } });
  const pagina = await contexto.newPage();
  await iniciarSesion(pagina, ADMIN2, VIEJA);
  await pagina.goto(`${URL}/usuarios/${idDe(ADMIN2)}/contrasenia`);
  await pagina.waitForSelector('#nuevaContrasenia');
  await llenar(pagina, NUEVA, NUEVA);
  await pagina.getByRole('button', { name: 'Guardar' }).click();
  await dialogo(pagina).getByRole('button', { name: 'Aceptar' }).click();
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 }).catch(() => {});
  revisar('17a. Al cambiar su propia contraseña regresa al inicio de sesión', pagina.url().includes('/iniciar-sesion'));
  revisar('17b. Muestra que debe entrar con la nueva contraseña', await aparece(pagina, 'Inicia sesión con tu nueva contraseña'));
  await pagina.screenshot({ path: `${CARPETA}/hu05-4-propia-cuenta.png`, fullPage: true });
  await pagina.goto(`${URL}/inicio`);
  await pagina.waitForURL('**/iniciar-sesion**', { timeout: 15000 }).catch(() => {});
  revisar('17c. Su sesión anterior ya no sirve', pagina.url().includes('/iniciar-sesion'));
  revisar('17d. Entra con la nueva contraseña', await intentarIniciar(pagina, ADMIN2, NUEVA));
  await contexto.close();
}

await navegador.close();
console.log('-----');
console.log(fallas === 0 ? 'HU-05 en navegador: todas las pruebas pasaron' : `HU-05 en navegador: ${fallas} prueba(s) fallaron`);
process.exit(fallas === 0 ? 0 : 1);
