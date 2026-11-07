# ETBOTFARMA WEB — Reglas del proyecto

> Este archivo lo leen automáticamente Claude Code y otras IA al abrir el repositorio.
> Contiene TODAS las decisiones del equipo. Síguelas exactamente.

## 1. Qué es

Sistema web de inventario de medicamentos (farmacia): usuarios y sesiones, catálogo de medicamentos,
lotes con caducidad (semáforo), salidas FEFO, bajas, stock mínimo, bitácora y exportación a Excel.
Equipo de estudiantes que está aprendiendo: **explica paso a paso, de forma sencilla y en español**,
y comenta el código en español.

## 2. Tecnologías (no agregar otras sin aprobación del equipo)

| Parte | Tecnología |
|-------|------------|
| Backend | .NET 10, ASP.NET Core, C# |
| CQRS | MediatR **12.5.0** (no actualizar: las versiones nuevas son de pago) |
| Validaciones | **FluentValidation 12.1.1** (`AbstractValidator<T>`), ejecutada automáticamente por MediatR (`ValidationBehavior`) |
| Base de datos | MySQL 8 + EF Core 9 con **Pomelo 9.0.0** (+ MySqlConnector 2.6.2 por seguridad). **Sin migraciones** |
| Frontend | Angular 20: standalone, `signal`, `inject()`, Reactive Forms, `@if` / `@for` |
| Pruebas | xUnit (unitarias, de arquitectura y de integración con `WebApplicationFactory`), scripts bash (punta a punta), Playwright (navegador) |
| Prohibido | AutoMapper, Swashbuckle, NgRx, librerías de UI, migraciones de EF |

## 3. Arquitectura (Clean Architecture + CQRS)

```
Angular → Api → Application → Domain
                    ↑
              Infrastructure
```

| Capa | Regla |
|------|-------|
| **Api** | El controller SOLO recibe el contrato JSON (`[FromBody]`), lo convierte en command/query, `await sender.Send(...)` y devuelve el Response. Sin `if`, sin lógica, sin validaciones, sin cookies, sin leer claims, sin try/catch, sin BD |
| **Application** | El handler SOLO COORDINA (pasos numerados y comentados). Usa interfaces, NUNCA DbContext ni SQL. No valida: los validadores los ejecuta MediatR |
| **Domain** | Reglas de negocio como métodos de las entidades (`usuario.PuedeIniciarSesion()`, `sesion.EstaVigente(...)`). Lanza `DomainException` (→ 422). No depende de nada |
| **Infrastructure** | SOLO obtiene, guarda y devuelve datos (repositorios, EF Core). Sin reglas de negocio |

> Explicación completa de la arquitectura, SOLID, CQRS, MediatR y FluentValidation: `docs/arquitectura.md`.

## 4. Estructura de carpetas: POR TIPO y luego POR HISTORIA

Las carpetas de tipo tienen nombres claros; dentro, una subcarpeta con el **nombre de la historia, sin "HU01"**.

```
backend/src/
├─ EtbotFarma.Api/
│  ├─ Controllers/<Historia>Controller.cs        ← UN controller por historia
│  ├─ Contracts/<Historia>/<CasoDeUso>Request.cs / <CasoDeUso>Response.cs
│  ├─ Seguridad/   Middleware/
├─ EtbotFarma.Application/
│  ├─ Commands/<Historia>/<CasoDeUso>Command.cs
│  ├─ Queries/<Historia>/<CasoDeUso>Query.cs
│  ├─ Validators/<Historia>/<CasoDeUso>Validator.cs
│  ├─ Handlers/<Historia>/<CasoDeUso>Handler.cs
│  ├─ Dtos/<Historia>/<CasoDeUso>Dto.cs
│  └─ Common/ (Behaviors, Interfaces, Interfaces/Repositorios, Exceptions, Messaging, Configuracion)
├─ EtbotFarma.Domain/Entities/                    ← compartidas (no van por historia)
└─ EtbotFarma.Infrastructure/Persistence/ (EtbotFarmaDbContext, Configuraciones/, Repositorios/), Seguridad/, Services/
backend/tests/
├─ EtbotFarma.Domain.Tests/Entities/
├─ EtbotFarma.Application.Tests/ Handlers/<Historia>/, Validators/<Historia>/, Common/, Arquitectura/, Falsos/
└─ EtbotFarma.Api.IntegrationTests/             ← endpoints contra MySQL (categoría "Integracion")
frontend/src/app/
├─ core/ (services/, guards/, interceptors/, models/)
├─ shared/components/
└─ features/<historia-en-kebab>/                  ← pantalla de la historia (.ts, .html, .scss)
```

### Nombre de carpeta por historia

| HU | Carpeta | Controller | Angular | Estado |
|----|---------|-----------|---------|--------|
| 01 | `InicioSesion` | `InicioSesionController` | `inicio-sesion` | Terminada |
| 02 | `CierreSesion` | `CierreSesionController` | `cierre-sesion` | Pendiente |
| 03 | `CreacionUsuarios` | `CreacionUsuariosController` | `creacion-usuarios` | Pendiente |
| 04 | `ConsultaUsuarios` | `ConsultaUsuariosController` | `consulta-usuarios` | Pendiente |
| 05 | `ReasignacionContrasena` | `ReasignacionContrasenaController` | `reasignacion-contrasena` | Pendiente |
| 06 | `RegistroMedicamentos` | `RegistroMedicamentosController` | `registro-medicamentos` | Pendiente |
| 07 | `EntradaLote` | `EntradaLoteController` | `entrada-lote` | Pendiente |
| 08 | `BusquedaMedicamentos` | `BusquedaMedicamentosController` | `busqueda-medicamentos` | Pendiente |
| 09 | `EdicionMedicamentos` | `EdicionMedicamentosController` | `edicion-medicamentos` | Pendiente |
| 10 | `SemaforoCaducidad` | `SemaforoCaducidadController` | `semaforo-caducidad` | Pendiente |
| 11 | `StockMinimo` | `StockMinimoController` | `stock-minimo` | Pendiente |
| 12 | `MedicamentosAgotados` | `MedicamentosAgotadosController` | `medicamentos-agotados` | Pendiente |
| 13 | `SalidaMedicamentos` | `SalidaMedicamentosController` | `salida-medicamentos` | Pendiente |
| 14 | `BajaLotes` | `BajaLotesController` | `baja-lotes` | Pendiente |
| 15 | `BitacoraMovimientos` | `BitacoraMovimientosController` | `bitacora-movimientos` | Pendiente |
| 16 | `ExportacionBitacora` | `ExportacionBitacoraController` | `exportacion-bitacora` | Pendiente |

Orden de desarrollo por fases: 1 (HU-01, 02) → 2 (03–05) → 3 (06–09) → 4 (10–12) → 5 (13–14) → 6 (15–16).
Detalle de las historias: `docs/historias-usuario.md` y el Excel `Historias_Usuario.xlsx` del Drive del equipo.

## 5. Convenciones de nombres (cortos y en español)

- Solo van en inglés: `Command`, `Query`, `Validator`, `Handler`, `Dto`, `Request`, `Response`, `Controller` y las carpetas de tipo.
- Clases: `<CasoDeUso>Command`, `<CasoDeUso>Validator`, ... (ej.: `IniciarSesionCommand`, `CrearUsuarioHandler`). Las clases NO llevan el número de historia.
- Namespace = ruta de carpetas (ej.: `EtbotFarma.Application.Commands.InicioSesion`).
- Rutas HTTP cortas en español. Cada controller declara su ruta: `[Route("api/sesiones")]` → `POST api/sesiones/iniciar`.
  Historias del mismo tema comparten prefijo (`api/sesiones`, `api/usuarios`, `api/medicamentos`, `api/inventario`, `api/bitacora`).
- Comentarios `///` en español indicando el criterio que cumple (ej.: `// HU-13 criterio 5: nunca stock negativo`).

## 6. Cómo se escribe cada pieza (copiar HU-01 como molde)

- **Command / Query**: `public sealed record XCommand(...) : ICommand<XDto>;` (`IQuery<XDto>` si solo lee).
- **Validator**: `public sealed class XValidator : AbstractValidator<XCommand>`; en el constructor una regla por campo: `RuleFor(c => c.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.");` (mensajes en español). Solo valida DATOS DE ENTRADA; las reglas de negocio van en Domain. Se registra solo y MediatR lo ejecuta antes del handler (`Common/Behaviors/ValidationBehavior.cs`) → `ValidacionException` → 400. Nunca crearlo con `new` en un handler.
- **Handler**: `public sealed class XHandler(dependencias...) : ICommandHandler<XCommand, XDto>`; pasos: obtener datos con repositorios → aplicar reglas del Domain → `await unitOfWork.GuardarCambiosAsync(ct)` UNA vez → devolver Dto. Fecha actual con `IDateTimeProvider` (UTC), nunca `DateTime.Now`.
- **Interfaces nuevas**: `Application/Common/Interfaces/` (repositorios en `.../Repositorios/`).
- **Entidades**: `Domain/Entities/`, heredan `Entity<int>`, `private set`, constructor `private` vacío (EF), método estático `Crear(...)`, reglas como métodos.
- **Infrastructure**: repositorio `XRepository(EtbotFarmaDbContext contexto) : IXRepository`; mapeo en `Persistence/Configuraciones/XConfiguracion.cs` con `ToTable(...)` y `HasColumnName(...)` para CADA propiedad (nombres snake_case reales de `database/etbotfarma_mysql.sql`); agregar `DbSet` y registrar en `DependencyInjection.cs`.
- **Contratos**: `Request` con propiedades nullables (`string?`); `Response` sin datos sensibles.
- **Controller**: `[ApiController] [Route("api/...")] public sealed class XController(ISender sender) : ControllerBase` (sin clases base propias; MediatR llega por el constructor). Todo está protegido por defecto (FallbackPolicy); `[AllowAnonymous]` solo si la historia lo dice; solo admin: `[Authorize(Roles = "Administrador")]`. El usuario actual lo obtiene el HANDLER con `IUsuarioActual` (nunca el controller). Cosas de HTTP (cookies) van detrás de una interfaz de Application implementada en `Api/Seguridad/`.
- **Errores** (ya los maneja `Middleware/GlobalExceptionHandler.cs`): `ValidacionException` 400 · `CredencialesInvalidasException` / `NoAutenticadoException` 401 · `AccesoDenegadoException` 403 · `NoEncontradoException` 404 · `DomainException` 422 · otros 500.
- **Angular**: pantalla en `features/<historia>/`; servicio en `core/services/` con `${environment.apiUrl}`; modelos en `core/models/` iguales a los contratos (camelCase); ruta lazy con `canActivate: [sesionGuard]`; confirmaciones/alertas exactamente como dice la historia (diálogo reutilizable en `shared/components/`).

## 6.1 Frontend: diseño compartido (obligatorio)

- Colores, letra (Inter), tamaños, espacios y sombras: SOLO con los tokens de `frontend/src/styles/_tokens.scss` (`var(--…)`). Nada de `#colores` ni `px` sueltos.
- Usar las clases de `frontend/src/styles/_componentes.scss` (`boton`, `campo`, `tarjeta`, `tabla`, `pantalla`, `estado-vacio`, `acciones`) y los componentes de `shared/components/`: `<app-alerta>`, `<app-confirmacion>` (toda confirmación previa), `<app-semaforo>` (estados de caducidad).
- Pantallas con sesión: ruta dentro de `children` del `LayoutComponent` en `app.routes.ts` y su `ruta` en `core/layout/menu.ts`.
- Responsive móvil primero con `@use 'quiebres' as *;` + `@include tableta {}` / `@include escritorio {}`.
- Accesible: `<label for>` en cada campo, `aria-invalid` en errores, íconos con `aria-hidden="true"`, nunca solo color.
- Los diseños de Stitch son referencia visual; los textos y campos los mandan los criterios de la historia.

## 7. Base de datos (MySQL, script `database/etbotfarma_mysql.sql`)

Tablas: `rol`, `usuario`, `sesion`, `medicamento`, `lote`, `movimiento`, `detalle_movimiento`, `auditoria`.
- El stock está en `lote.stock`, no en `medicamento`.
- Toda entrada/salida/baja: 1 fila en `movimiento` + N en `detalle_movimiento`, en la MISMA transacción que el cambio de stock.
- Cambios administrativos → `auditoria` (nunca contraseñas).
- Nada se borra físicamente (`ON DELETE RESTRICT`): `activo = false` o `stock = 0`.
- Semáforo (meses calendario): Verde > 12 · Amarillo > 3 y ≤ 12 · Rojo ≤ 3 (incluye caducados). Se calcula en el Domain.
- Si una historia necesita cambiar tablas: modificar el script y `database/pruebas/` y explicarlo.

## 8. Pruebas (obligatorias en cada historia)

- Domain: una prueba por regla nueva (`backend/tests/EtbotFarma.Domain.Tests/Entities/`).
- Application: handler con clases falsas (`Falsos/Falsos.cs`) y validator con `TestValidate` de FluentValidation (`Handlers/<Historia>/`, `Validators/<Historia>/`). Las pruebas de `Arquitectura/` revisan nombres, carpetas y dependencias: no las desactives.
- Integración (cuando la historia tiene endpoint): `backend/tests/EtbotFarma.Api.IntegrationTests/` con `[Trait("Category", "Integracion")]` (corren en GitHub con MySQL).
- Nombres en español: `Con_stock_insuficiente_no_registra_la_salida()`.
- Comandos: `dotnet test backend/EtbotFarma.slnx --filter "Category!=Integracion"` (sin MySQL) y `npm run build` (en `frontend`). GitHub Actions corre además las de integración, MySQL real y navegador.

## 9. Git

- Nunca trabajar en `main` ni `develop`. Una rama por historia desde `develop`: `feature/<historia-en-kebab>` (ej.: `feature/cierre-sesion`).
- Commits: `feat(HU-02): ...`, `fix(HU-02): ...`, `test: ...`, `docs: ...`.
- Pull Request hacia `develop`; se une solo con todas las pruebas en verde.
- Nunca subir `appsettings.Local.json` (contraseñas locales).

## 10. Ejecutar localmente

1. MySQL 8: ejecutar `database/etbotfarma_mysql.sql`.
2. Copiar `backend/src/EtbotFarma.Api/appsettings.Local.example.json` como `appsettings.Local.json` (contraseña de MySQL y del usuario `admin`).
3. `cd backend && dotnet run --project src/EtbotFarma.Api` (http://localhost:5080).
4. `cd frontend && npm install && npm start` (http://localhost:4200).

## 11. Pendientes por definir con el equipo

- Roles contradictorios en HU-14 (baja) y HU-15 (bitácora) entre el índice y sus criterios.
- Dato que se pide en la salida de un medicamento controlado (HU-13 criterio 1).
- Si se permite registrar un lote que caduca en menos de 3 meses (HU-07).
