# Arquitectura del backend

Monolito con **Clean Architecture (separación por capas) + CQRS con MediatR + FluentValidation**.
Esta es la **plantilla obligatoria** para todas las historias de usuario. El molde real es la **HU-01 (InicioSesion)**.

## 1. Capas y regla de dependencias

```
Angular  →  Api  →  Application  →  Domain
                        ↑
                  Infrastructure
```

| Proyecto | Responsabilidad | Depende de |
|----------|-----------------|------------|
| **EtbotFarma.Domain** | Entidades y reglas de negocio (`Usuario.PuedeIniciarSesion()`, `Sesion.EstaVigente(...)`). Lanza `DomainException` | Nada |
| **EtbotFarma.Application** | Casos de uso: Commands, Queries, Handlers, Validators, DTOs, interfaces de repositorios y servicios, `ValidationBehavior` | Domain |
| **EtbotFarma.Infrastructure** | Implementa las interfaces de Application: SQL a mano con MySqlConnector (`ConexionMySql`, repositorios), hash de contraseñas, tokens, reloj | Application, Domain |
| **EtbotFarma.Api** | Controllers delgados, Contracts (Request/Response), Middleware de errores, seguridad HTTP (cookie, usuario actual), configuración | Application, Infrastructure |

Las capas internas **nunca** conocen a las externas. Una prueba automática lo vigila
(`tests/EtbotFarma.Application.Tests/Arquitectura/ArquitecturaTests.cs`).

## 2. Flujo de una petición: de Angular a MySQL (HU-01)

```
1. Angular            POST /api/sesiones/iniciar   { nombreUsuario, contrasenia }
2. Controller         InicioSesionController: IniciarSesionRequest → IniciarSesionCommand → sender.Send(...)
3. MediatR            busca el pipeline y el handler del command
4. ValidationBehavior ejecuta IniciarSesionValidator (FluentValidation)
                      ¿errores? → ValidacionException → HTTP 400 (el handler NO se ejecuta)
5. Handler            IniciarSesionHandler coordina:
                        IUsuarioRepository → UsuarioRepository → SELECT con parámetros → MySQL (tabla usuario)
                        reglas del Domain: usuario.PuedeIniciarSesion()
                        IPasswordHasher, ITokenSesionService, Sesion.Iniciar(...)
                        ISesionRepository (INSERT) + IUnitOfWork (COMMIT) → MySQL (tabla sesion)
                        ICookieSesionService → cookie segura (implementada en Api)
6. Handler            devuelve UsuarioSesionDto
7. Controller         UsuarioSesionDto → IniciarSesionResponse → HTTP 200
   (Errores)          GlobalExceptionHandler convierte cualquier excepción en la respuesta HTTP correcta
```

## 3. CQRS y MediatR

| Tipo | Para qué | Interfaz | Su handler implementa | Ejemplo real |
|------|----------|----------|------------------------|--------------|
| **Command** | Operación que **modifica** datos | `ICommand<TResponse>` | `ICommandHandler<TCommand, TResponse>` | `IniciarSesionCommand`, `ValidarSesionCommand` |
| **Query** | Operación que **solo lee** | `IQuery<TResponse>` | `IQueryHandler<TQuery, TResponse>` | `ObtenerSesionActualQuery`, `ObtenerEstadoQuery` |

- Las interfaces están en `Application/Common/Messaging/` y heredan de `IRequest` / `IRequestHandler` de **MediatR 12.5.0**.
- Los controllers reciben `ISender` (MediatR) **por constructor** y usan `sender.Send(...)`.
- MediatR registra los handlers **solo** (`AddMediatR` en `Application/DependencyInjection.cs`).
- Un handler por Command o Query. Nunca leer y escribir en el mismo handler.

## 4. Validaciones con FluentValidation

```
sender.Send(command)
      ↓
ValidationBehavior<TRequest, TResponse>        (Application/Common/Behaviors)
      ↓ 1. Recibe todos los IValidator<TCommand> registrados
      ↓ 2. Ejecuta ValidateAsync en cada uno y junta los errores
      ↓ 3. ¿Errores? Sí → ValidacionException → HTTP 400 { "errores": [...] }
      ↓              No → continúa
   Handler (sin validaciones)
```

- Los validators se registran solos con `services.AddValidatorsFromAssembly(...)`.
- Validator = **datos de entrada** (obligatorio, longitud, formato, número positivo).
  Regla de negocio (no hay stock suficiente, el lote está caducado) = **Domain** → `DomainException` → 422.
- Nadie crea un validator con `new` (salvo las pruebas unitarias del propio validator).

## 5. Manejo de errores (`Api/Middleware/GlobalExceptionHandler.cs`)

| Excepción | HTTP | Cuándo |
|-----------|------|--------|
| `ValidacionException` | 400 | Datos de entrada incorrectos (lista `errores`) |
| `CredencialesInvalidasException` | 401 | Usuario o contraseña incorrectos (mensaje genérico) |
| `NoAutenticadoException` | 401 | No hay sesión válida |
| `AccesoDenegadoException` | 403 | El rol no permite la acción |
| `NoEncontradoException` | 404 | El registro no existe |
| `DomainException` | 422 | Regla de negocio no cumplida |
| Cualquier otra | 500 | Error inesperado: no se muestran detalles al cliente, solo se registra en el log |

Los controllers no tienen `try/catch`.

## 6. Principios SOLID aplicados

| Principio | Cómo se aplica (ejemplo real) |
|-----------|-------------------------------|
| **S** — Responsabilidad única | Cada archivo hace una cosa: `InicioSesionController` (HTTP), `IniciarSesionValidator` (entrada), `IniciarSesionHandler` (coordinar), `UsuarioRepository` (datos), `Usuario` (reglas), `CookieSesionService` (cookie) |
| **O** — Abierto/cerrado | Una historia nueva agrega archivos nuevos (command, validator, handler, controller) sin modificar los existentes; MediatR y FluentValidation los encuentran solos |
| **L** — Sustitución de Liskov | Cualquier implementación de `IUsuarioRepository` sirve: la real (MySQL) o la falsa de las pruebas (`Falsos/UsuarioRepositoryFalso.cs`) |
| **I** — Segregación de interfaces | Interfaces pequeñas: `IPasswordHasher`, `ITokenSesionService`, `ICookieSesionService`, `IUsuarioActual`, `IDateTimeProvider`, un repositorio por entidad |
| **D** — Inversión de dependencias | Los handlers dependen de interfaces de Application; Infrastructure y Api las implementan y se inyectan por constructor (`DependencyInjection.cs`, `Program.cs`) |

Sin herencia entre clases concretas: controllers heredan solo `ControllerBase` (de ASP.NET),
validators solo `AbstractValidator<T>` (de FluentValidation), handlers y commands no heredan de nada.

## 7. Convenciones de nombres y carpetas

**Una carpeta por historia** (nombre de la historia sin número) y dentro **una subcarpeta por tipo**:

| Pieza | Carpeta | Nombre | Ejemplo HU-01 |
|-------|---------|--------|---------------|
| Command | `Application/<Historia>/Commands/` | `<CasoDeUso>Command` | `IniciarSesionCommand` |
| Query | `Application/<Historia>/Queries/` | `<CasoDeUso>Query` | `ObtenerSesionActualQuery` |
| Validator | `Application/<Historia>/Validators/` | `<CasoDeUso>Validator` | `IniciarSesionValidator` |
| Handler | `Application/<Historia>/Handlers/` | `<CasoDeUso>Handler` | `IniciarSesionHandler` |
| DTO | `Application/<Historia>/Dtos/` | `<Algo>Dto` | `UsuarioSesionDto` |
| Interfaz | `Application/Common/Interfaces/(Repositorios/)` | `I<Algo>` | `IUsuarioRepository` |
| Repositorio | `Infrastructure/Persistence/Repositorios/` | `<Entidad>Repository` | `UsuarioRepository` |
| Controller | `Api/Historias/<Historia>/` | `<Historia>Controller` | `InicioSesionController` |
| Contracts | `Api/Historias/<Historia>/` (junto al controller) | `<CasoDeUso>Request` / `Response` | `IniciarSesionRequest` |

La tabla con la carpeta de cada una de las 16 historias está en `backend/src/EtbotFarma.Application/README.md`.

## 8. Cómo agregar una historia de usuario (plantilla obligatoria)

1. Leer la descripción y los criterios de aceptación. Decidir qué operaciones son **Command** (modifican) y cuáles **Query** (leen).
2. **Domain**: crear o completar la entidad en `Domain/Entities/` con sus reglas como métodos.
3. **Application**: por cada operación, en `Application/<Historia>/<Tipo>/`:
   `XCommand` (o `XQuery`) + `XValidator` (si recibe datos) + `XHandler` + `XDto` (si devuelve datos).
   Si necesita la base de datos, crear `IXRepository` en `Common/Interfaces/Repositorios/`.
4. **Infrastructure**: `XRepository(ConexionMySql conexion)` con SQL parametrizado (columnas reales de `database/etbotfarma_mysql_v2.sql`) y registro en `DependencyInjection.cs`.
5. **Api**: carpeta `Historias/<Historia>/` con `<Historia>Controller` (`ISender` por constructor) y sus Request/Response.
6. **Pruebas**: del validator (`TestValidate`), del handler (con los falsos de `Falsos/`, un archivo por clase) y de integración del endpoint cuando aplique.
7. Crear solo los archivos que la historia necesita: nada de archivos vacíos.

## 9. Base de datos

- MySQL 8 con **SQL a mano** (MySqlConnector, sin ORM). No hay referencias a SQL Server ni a Entity Framework (una prueba lo vigila).
- `ConexionMySql`: una conexión y una transacción por petición. Los repositorios escriben dentro de ella; el handler confirma con `IUnitOfWork.GuardarCambiosAsync` (COMMIT). Si hay un error antes, ROLLBACK automático.
- La base se crea con `database/etbotfarma_mysql_v2.sql`; una base existente se actualiza con `database/migracion_v1_a_v2.sql`. Detalle de tablas, reglas y diagrama: `docs/base-de-datos.md`.
- La cadena de conexión NO está en el código: va en `appsettings.Local.json` (no se sube) o en variables de entorno (`ConnectionStrings__EtbotFarma`).

## 10. Pruebas

| Proyecto | Qué prueba | Necesita MySQL |
|----------|-----------|----------------|
| `EtbotFarma.Domain.Tests` | Reglas de las entidades | No |
| `EtbotFarma.Application.Tests` | Validators, handlers, pipeline real de MediatR y reglas de arquitectura | No |
| `EtbotFarma.Api.IntegrationTests` | Endpoints completos en memoria, repositorios SQL y transacciones (COMMIT/ROLLBACK) contra MySQL (categoría `Integracion`) | Sí |
| `EtbotFarma.Api.IntegrationTests/ArquitecturaInfraestructuraTests` | Infrastructure no usa EF Core ni otro ORM | No |
| `backend/tests/e2e/*.sh` y `frontend/e2e/*.mjs` | API y navegador de punta a punta | Sí |

En GitHub Actions: el job **Backend** corre las pruebas sin MySQL; el job **Punta a punta** corre las de integración y los scripts.

## 11. Decisiones

| Decisión | Motivo |
|----------|--------|
| MediatR 12.5.0 | Última versión con licencia libre (Apache 2.0) |
| FluentValidation 12.1.1 | Estándar de .NET para validar entradas; gratuita (Apache 2.0) |
| Un controller por historia | Cada integrante trabaja en su propio archivo (menos conflictos en Git) |
| Cookie de sesión vía `ICookieSesionService` | El controller no tiene lógica; el token nunca viaja en el JSON |
| `IUsuarioActual` | Los handlers saben quién hizo la petición sin leer HTTP (servirá para la bitácora) |
| SQL a mano, sin ORM | Requisito del proyecto: el SQL queda visible y se puede explicar; el esquema vive en scripts SQL |
| FEFO en salidas | La HU-13 lo pide: sale primero lo que caduca primero |
| `IDateTimeProvider` | Permite probar el semáforo de caducidad con fechas fijas |

## 12. Manejo de sesiones (cookie, no token en JavaScript)

Angular y .NET usan **un solo mecanismo: una cookie de sesión**. No se usan tokens JWT ni encabezados `Authorization`.

| Paso | Dónde | Qué pasa |
|------|-------|----------|
| 1. Iniciar sesión | `IniciarSesionHandler` | Genera un token aleatorio, guarda SOLO su hash SHA-256 en `sesion.token_hash` y la fecha de expiración (+20 min) |
| 2. Entregar el token | `ICookieSesionService` → `Api/Seguridad/CookieSesionService` | Lo escribe en la cookie `etbotfarma_sesion`: **HttpOnly** (JavaScript no la lee), **Secure**, **SameSite=Strict**. El JSON de respuesta NO lleva el token |
| 3. Enviar la cookie | Angular `core/interceptors/credenciales.interceptor.ts` | `withCredentials: true` hace que el navegador mande la cookie en cada petición |
| 4. Validar en cada petición | `Api/Seguridad/SesionAuthenticationHandler` → `ValidarSesionCommand` → `ValidarSesionHandler` | Busca la sesión por el hash; si está cerrada, vencida o el usuario inactivo → la cierra y responde 401; si no, recorre la expiración |
| 5. ¿Quién soy? | `GET api/sesiones/actual` → `ObtenerSesionActualHandler` (con `IUsuarioActual`) | Devuelve id, usuario, nombre completo y rol para Angular |
| 6. Sesión vencida en Angular | `core/interceptors/sesion-expirada.interceptor.ts` | Ante un 401 manda a `/iniciar-sesion?expirada=1` |

Por qué cookie HttpOnly y no un token guardado en Angular: el token nunca queda al alcance de JavaScript
(protege contra robo por XSS) y el servidor puede cerrar o vencer la sesión cuando quiera (se guarda en MySQL).

## 13. Conexión con MySQL (SQL a mano)

```
Handler ──usa──> ISesionRepository / IUnitOfWork          (interfaces, Application)
                        │ implementadas por
                        ▼
                 SesionRepository(ConexionMySql)          (Infrastructure/Persistence/Repositorios)
                        │ SQL parametrizado (@token_hash ...)
                        ▼
                 ConexionMySql                            (Infrastructure/Persistence)
                   - 1 conexión + 1 transacción por petición (BEGIN al primer uso)
                   - GuardarCambiosAsync → COMMIT
                   - fin de la petición sin COMMIT → ROLLBACK
                        ▼
                     MySQL 8 (MySqlConnector 2.6.2)
```

- `ConexionMySql` se registra como **Scoped** (una por petición) en `Infrastructure/DependencyInjection.cs`; `IUnitOfWork` apunta a la misma instancia.
- Los comandos y lectores se liberan con `await using`.
- La conexión se abre solo cuando un repositorio la necesita (las peticiones sin base de datos no la abren).
- Cada repositorio convierte filas en entidades con `Entidad.Reconstruir(...)`.

## 14. Decisiones de organización

| Tema | Decisión | Motivo |
|------|----------|--------|
| Carpetas de Application y Api | **Por historia** (`InicioSesion/`) y dentro **por tipo** (`Commands/`, `Queries/`, `Validators/`, `Handlers/`, `Dtos/`); en Api, `Historias/<Historia>/` con el controller y sus contratos | Decisión del equipo: cada integrante trabaja en la carpeta de su historia (menos choques en Git) y dentro se ve de inmediato qué tipo de archivo es cada uno |
| Validator para queries sin datos | No se crea (ej.: `ObtenerSesionActualQuery`) | No hay nada que validar; no se crean archivos vacíos |
| Clase base `Entity<TId>` en Domain | Se conserva | Da a todas las entidades su `Id` en un solo lugar; es la única herencia del Domain y es abstracta (no se puede crear sola) |
| Nombres de Infrastructure | `ConexionMySql`, `Persistence/Repositorios/` | En español, como el resto del proyecto |
