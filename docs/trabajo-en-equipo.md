# Guía de trabajo en equipo

Cómo programar tu historia de usuario **sin estorbar a tus compañeras**.
Las reglas de código están en [`CLAUDE.md`](../CLAUDE.md). Esta guía explica la parte de **equipo y Git**.

---

## 1. Antes de empezar una historia

1. Revisa en [`docs/historias-usuario.md`](historias-usuario.md) que la historia esté `Pendiente` y que las de su
   fase anterior estén terminadas (por ejemplo, HU-03 necesita HU-01).
2. Crea tu rama **desde `develop`** (nunca desde `main`, que tiene una versión más vieja):

   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/cierre-sesion
   ```

   El nombre de la rama es `feature/` + la carpeta de la historia en kebab-case (tabla de `CLAUDE.md` §4).
3. En tu **primer commit**, pon tu nombre en la columna *Responsable* de `docs/historias-usuario.md` y cambia
   el estado a `En progreso`. Súbelo (`git push -u origin feature/cierre-sesion`) para que todas lo vean.
4. Instala las dependencias igual que el resto del equipo:

   ```bash
   cd frontend
   npm ci
   ```

   No uses `npm install <paquete>` ni agregues paquetes NuGet sin aprobación del equipo (`CLAUDE.md` §2).

---

## 2. Archivos NUEVOS de tu historia (aquí no hay choques)

Casi todo lo que programas va en carpetas **con el nombre de tu historia**, así que nadie más las toca.
Ejemplo para HU-02 (`CierreSesion`):

| Capa | Dónde |
|---|---|
| Application | `CierreSesion/` con `Commands/`, `Queries/`, `Validators/`, `Handlers/`, `Dtos/` (solo las que necesites) |
| Api | `Historias/CierreSesion/` (`CierreSesionController.cs` + sus Request/Response) |
| Pruebas | `tests/EtbotFarma.Application.Tests/CierreSesion/Handlers/`, `.../CierreSesion/Validators/`; integración en `tests/EtbotFarma.Api.IntegrationTests/Historias/CierreSesion/` |
| Falsos nuevos | `tests/EtbotFarma.Application.Tests/Falsos/<Nombre>Falso.cs`: un archivo por clase (ver `Falsos/README.md`) |
| Angular | `frontend/src/app/features/cierre-sesion/` |

Crea solo los archivos que tu historia necesita. Nada de carpetas o archivos vacíos "por si acaso".

---

## 3. Archivos COMPARTIDOS (aquí sí puede haber conflictos)

Estos archivos los editan varias historias. La regla general:
**agrega tus líneas al FINAL de la sección correspondiente, con un comentario `// HU-XX`, y no reordenes,
reformatees ni borres las líneas de otras.**

| Archivo | Qué agregas | Ejemplo |
|---|---|---|
| `backend/src/EtbotFarma.Infrastructure/DependencyInjection.cs` | El registro de tu repositorio o servicio, en la sección `Repositorios` o `Seguridad` | `services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();   // HU-06` |
| `backend/src/EtbotFarma.Api/Program.cs` | Solo si creas un servicio de la capa Api (algo de HTTP) | Igual que `ICookieSesionService` |
| `backend/src/EtbotFarma.Api/Middleware/GlobalExceptionHandler.cs` | Casi nunca: ya maneja 400, 401, 403, 404 y 422. Usa las excepciones que ya existen | — |
| `Domain/Entities/*.cs` | Las entidades son de todas. Si tu historia necesita un método nuevo en una entidad que ya existe (por ejemplo `Usuario`), **agrégalo sin cambiar los métodos que ya existen** | `usuario.CambiarContrasenia(...)` para HU-05 |
| `Application/Common/Interfaces/Repositorios/I*Repository.cs` | Igual: agrega métodos nuevos; no cambies los que ya usan otras historias. Agrega también el método al repositorio real y a su falso | `IUsuarioRepository.AgregarAsync(...)` para HU-03 |
| `frontend/src/app/app.routes.ts` | Tu ruta dentro de `children` del `LayoutComponent` | `{ path: 'cierre-sesion', loadComponent: ... }` |
| `frontend/src/app/core/layout/menu.ts` | Si tu pantalla corresponde a una opción del menú (la opción ya existe y lista sus historias en `historias`), escribe su `ruta`. No crees opciones nuevas sin acordarlo | `ruta: '/salida-medicamentos'` |
| `database/etbotfarma_mysql_v2.sql` | Solo si tu historia necesita cambiar una tabla. Sigue `CLAUDE.md` §7 (script de migración + pruebas + `docs/base-de-datos.md`) y **avisa al equipo antes** | — |
| `docs/historias-usuario.md` | Solo la fila de TU historia | Estado y Responsable |

> **Historias que tocan lo mismo:** HU-03, HU-04 y HU-05 usan `Usuario` e `IUsuarioRepository`;
> HU-06, HU-07, HU-08 y HU-09 usan `Medicamento` y `Lote`. Si les tocan historias de la misma fase,
> **pónganse de acuerdo** sobre quién crea primero la entidad o la interfaz, y la otra persona actualiza su rama
> después de que se una el primer Pull Request (sección 4).

---

## 4. Mantener tu rama al día con `develop`

Hazlo **al menos una vez al día** y siempre antes de abrir tu Pull Request:

```bash
git checkout feature/cierre-sesion
git fetch origin
git merge origin/develop
```

- Si Git dice `Already up to date` o hace el merge solo: listo. Corre las pruebas (sección 6).
- Si dice `CONFLICT`: sigue la sección 5.

> Usamos `merge` (no `rebase`) porque es más fácil de entender y no reescribe tu historial.

---

## 5. Cómo resolver un conflicto, paso a paso

Un **conflicto** pasa cuando tú y otra persona cambiaron **las mismas líneas** de un archivo, y Git no sabe con
cuál quedarse. Git marca el archivo así:

```csharp
<<<<<<< HEAD
        services.AddScoped<ISesionRepository, SesionRepository>();
        services.AddScoped<ILoteRepository, LoteRepository>();            // HU-07 (tu cambio)
=======
        services.AddScoped<ISesionRepository, SesionRepository>();
        services.AddScoped<IMedicamentoRepository, MedicamentoRepository>(); // HU-06 (lo que ya estaba en develop)
>>>>>>> origin/develop
```

1. `git status` te dice qué archivos tienen conflicto (`both modified`).
2. Abre cada archivo (VS Code muestra los botones *Accept Current / Incoming / Both*).
3. En los archivos compartidos, casi siempre la respuesta es **quedarte con LAS DOS** líneas:

   ```csharp
           services.AddScoped<ISesionRepository, SesionRepository>();
           services.AddScoped<IMedicamentoRepository, MedicamentoRepository>(); // HU-06
           services.AddScoped<ILoteRepository, LoteRepository>();            // HU-07
   ```

4. Borra las marcas `<<<<<<<`, `=======` y `>>>>>>>`.
5. Compila y corre las pruebas (sección 6). **Si no compila, el conflicto no está bien resuelto.**
6. Termina el merge:

   ```bash
   git add .
   git commit -m "merge: develop en feature/cierre-sesion"
   git push
   ```

Si no estás segura de qué conservar, **no borres el código de otra persona**: pregunta en el grupo.

---

## 6. Antes de abrir el Pull Request

Corre esto en tu computadora (lo mismo que revisa GitHub Actions):

```bash
cd backend
dotnet build EtbotFarma.slnx
dotnet test EtbotFarma.slnx --filter "Category!=Integracion"
```

```bash
cd frontend
npm run build
```

- Si una prueba de `Arquitectura/` falla, **no la desactives**: te está diciendo que un archivo tiene un nombre,
  una carpeta o una dependencia que rompe las reglas. El mensaje dice cuál.
- Las pruebas de integración (con MySQL) las corre GitHub. Si tienes MySQL local, puedes correr todas con
  `dotnet test EtbotFarma.slnx`.

Luego abre el Pull Request **hacia `develop`** (no hacia `main`). GitHub llena la descripción con una lista
de revisión: complétala.

---

## 7. Revisar el Pull Request de una compañera

- Que todos los checks de GitHub Actions estén en verde.
- Que los archivos estén en las carpetas de su historia y los compartidos solo tengan líneas **agregadas**.
- Que cada criterio de aceptación tenga su prueba.
- Que no haya contraseñas, `appsettings.Local.json` ni `node_modules` en los cambios.
- Comenta con respeto y en concreto: "En `X.cs` línea 20 falta..." en lugar de "está mal".

El Pull Request se une a `develop` solo cuando todo está en verde y al menos una compañera lo revisó.

---

## 8. `develop` → `main`

`main` es la versión estable. Cuando termina una fase completa (por ejemplo, la fase 1: HU-01 y HU-02),
la coordinadora abre un Pull Request de `develop` hacia `main`. Nadie programa directo en `main`.
