// =====================================================================
// Prueba en navegador de HU-01 (se ejecuta en GitHub Actions con Playwright).
// Requiere: API en :5080, Angular en :4200 y MySQL con la base etbotfarma.
// Guarda capturas de pantalla en e2e/capturas/.
// =====================================================================
import { chromium } from 'playwright';
import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';

const URL = 'http://localhost:4200';
const CONTRASENIA = process.env.CONTRASENIA ?? 'Prueba123!';
const CARPETA = 'e2e/capturas';
mkdirSync(CARPETA, { recursive: true });

let fallas = 0;
function revisar(descripcion, condicion) {
  console.log(`${condicion ? 'OK   ' : 'FALLA'}  ${descripcion}`);
  if (!condicion) fallas++;
}

const navegador = await chromium.launch();
const pagina = await navegador.newPage({ viewport: { width: 1000, height: 700 } });

// 1. Sin sesión, una pantalla protegida manda al login (criterio 5)
await pagina.goto(`${URL}/inicio`);
await pagina.waitForURL('**/iniciar-sesion**');
revisar('Sin sesión, /inicio redirige a /iniciar-sesion', pagina.url().includes('/iniciar-sesion'));
await pagina.screenshot({ path: `${CARPETA}/1-login.png` });

// 2. Campos obligatorios (criterio 1)
async function aparece(texto) {
  try {
    await pagina.getByText(texto).waitFor({ timeout: 5000 });
    return true;
  } catch {
    return false;
  }
}

await pagina.click('button[type=submit]');
revisar('Muestra "El nombre de usuario es obligatorio."', await aparece('El nombre de usuario es obligatorio.'));
revisar('Muestra "La contraseña es obligatoria."', await aparece('La contraseña es obligatoria.'));
await pagina.screenshot({ path: `${CARPETA}/2-campos-obligatorios.png` });

// 3. Contraseña incorrecta: mensaje genérico (criterio 4)
await pagina.fill('#nombreUsuario', 'admin');
await pagina.fill('#contrasenia', 'Equivocada');
await pagina.click('button[type=submit]');
await pagina.getByText('Usuario o contraseña incorrectos.').waitFor({ timeout: 10000 });
revisar('Contraseña incorrecta: "Usuario o contraseña incorrectos."', true);
await pagina.screenshot({ path: `${CARPETA}/3-credenciales-incorrectas.png` });

// 4. Credenciales correctas: entra al módulo inicial (criterio 3)
await pagina.fill('#contrasenia', CONTRASENIA);
await pagina.click('button[type=submit]');
await pagina.waitForURL('**/inicio');
await pagina.getByText('Hola, Administrador del Sistema').waitFor({ timeout: 10000 });
revisar('Credenciales correctas: entra a /inicio y saluda al admin', true);
await pagina.screenshot({ path: `${CARPETA}/4-inicio.png` });

// 5. La cookie de sesión es HttpOnly (JavaScript no la puede leer)
const cookieVisibleParaJs = await pagina.evaluate(() => document.cookie.includes('etbotfarma_sesion'));
revisar('JavaScript NO puede leer la cookie de sesión', !cookieVisibleParaJs);

// 6. Recargar la página conserva la sesión
await pagina.reload();
await pagina.getByText('Hola, Administrador del Sistema').waitFor({ timeout: 10000 });
revisar('Al recargar, la sesión se conserva', pagina.url().includes('/inicio'));

// 7. Si ya hay sesión, /login manda al inicio
await pagina.goto(`${URL}/iniciar-sesion`);
await pagina.waitForURL('**/inicio');
revisar('Con sesión, /iniciar-sesion redirige a /inicio', pagina.url().includes('/inicio'));

// 8. Sesión expirada por inactividad (criterio 6): se simulan 30 minutos sin actividad
execSync(`mysql -h127.0.0.1 -uroot -proot etbotfarma -e "UPDATE sesion SET ultima_actividad = UTC_TIMESTAMP() - INTERVAL 30 MINUTE"`);
await pagina.goto(`${URL}/inicio`);
await pagina.waitForURL('**/iniciar-sesion**');
revisar('Sesión expirada: regresa a iniciar sesión', pagina.url().includes('/iniciar-sesion'));
await pagina.getByText('Tu sesión expiró por inactividad').waitFor({ timeout: 10000 });
revisar('Sesión expirada: muestra "Tu sesión expiró por inactividad"', true);
await pagina.screenshot({ path: `${CARPETA}/5-sesion-expirada.png` });

await navegador.close();

console.log('-----');
if (fallas > 0) {
  console.log(`${fallas} prueba(s) fallaron`);
  process.exit(1);
}
console.log('HU-01 en navegador: todas las pruebas pasaron');
