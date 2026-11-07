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
| **EtbotFarma.Infrastructure** | Implementa las interfaces de Application: EF Core + MySQL (Pomelo), repositorios, hash de contraseñas, tokens, reloj | Application, Domain |
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
                        IUsuarioRepository → UsuarioRepository → EF Core → MySQL (tabla usuario)
                        reglas del Domain: usuario.PuedeIniciarSesion()
                        IPasswordHasher, ITokenSesionService, Sesion.Iniciar(...)
                        ISesionRepository + IUnitOfWork → MySQL (tabla sesion)
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
| **L** — Sustitución de Liskov | Cualquier implementación de `IUsuarioRepository` sirve: la real (MySQL) o la falsa de las pruebas (`Falsos.cs`) |
| **I** — Segregación de interfaces | Interfaces pequeñas: `IPasswordHasher`, `ITokenSesionService`, `ICookieSesionService`, `IUsuarioActual`, `IDateTimeProvider`, un repositorio por entidad |
| **D** — Inversión de dependencias | Los handlers dependen de interfaces de Application; Infrastructure y Api las implementan y se inyectan por constructor (`DependencyInjection.cs`, `Program.cs`) |

Sin herencia entre clases concretas: controllers heredan solo `ControllerBase` (de ASP.NET),
validators solo `AbstractValidator<T>` (de FluentValidation), handlers y commands no heredan de nada.

## 7. Convenciones de nombres y carpetas

Carpetas **por tipo** y dentro **una subcarpeta por historia** (nombre de la historia sin número):

| Pieza | Carpeta | Nombre | Ejemplo HU-01 |
|-------|---------|--------|---------------|
| Command | `Application/Commands/<Historia>/` | `<CasoDeUso>Command` | `IniciarSesionCommand` |
| Query | `Application/Queries/<Historia>/` | `<CasoDeUso>Query` | `ObtenerSesionActualQuery` |
| Validator | `Application/Validators/<Historia>/` | `<CasoDeUso>Validator` | `IniciarSesionValidator` |
| Handler | `Application/Handlers/<Historia>/` | `<CasoDeUso>Handler` | `IniciarSesionHandler` |
| DTO | `Application/Dtos/<Historia>/` | `<Algo>Dto` | `UsuarioSesionDto` |
| Interfaz | `Application/Common/Interfaces/(Repositorios/)` | `I<Algo>` | `IUsuarioRepository` |
| Repositorio | `Infrastructure/Persistence/Repositorios/` | `<Entidad>Repository` | `UsuarioRepository` |
| Controller | `Api/Controllers/` | `<Historia>Controller` | `InicioSesionController` |
| Contracts | `Api/Contracts/<Historia>/` | `<CasoDeUso>Request` / `Response` | `IniciarSesionRequest` |

La tabla con la carpeta de cada una de las 16 historias está en `backend/src/EtbotFarma.Application/README.md`.

## 8. Cómo agregar una historia de usuario (plantilla obligatoria)

1. Leer la descripción y los criterios de aceptación. Decidir qué operaciones son **Command** (modifican) y cuáles **Query** (leen).
2. **Domain**: crear o completar la entidad en `Domain/Entities/` con sus reglas como métodos.
3. **Application**: por cada operación, en su carpeta de historia:
   `XCommand` (o `XQuery`) + `XValidator` (si recibe datos) + `XHandler` + `XDto` (si devuelve datos).
   Si necesita la base de datos, crear `IXRepository` en `Common/Interfaces/Repositorios/`.
4. **Infrastructure**: `XRepository`, `XConfiguracion` (mapeo a la tabla real del script SQL), `DbSet` y registro en `DependencyInjection.cs`.
5. **Api**: `<Historia>Controller` con `ISender` por constructor + `Contracts/<Historia>/` (Request/Response).
6. **Pruebas**: del validator (`TestValidate`), del handler (con los falsos de `Falsos.cs`) y de integración del endpoint cuando aplique.
7. Crear solo los archivos que la historia necesita: nada de archivos vacíos.

## 9. Base de datos

- MySQL 8 con EF Core 9 + Pomelo 9.0.0 (`Infrastructure/DependencyInjection.cs`). No hay referencias a SQL Server.
- La base se crea con el script `database/etbotfarma_mysql.sql` (decisión del equipo: **sin migraciones de EF**, el script es la única fuente del esquema).
- La cadena de conexión NO está en el código: va en `appsettings.Local.json` (no se sube) o en variables de entorno (`ConnectionStrings__EtbotFarma`).

## 10. Pruebas

| Proyecto | Qué prueba | Necesita MySQL |
|----------|-----------|----------------|
| `EtbotFarma.Domain.Tests` | Reglas de las entidades | No |
| `EtbotFarma.Application.Tests` | Validators, handlers, pipeline real de MediatR y reglas de arquitectura | No |
| `EtbotFarma.Api.IntegrationTests` | Endpoints completos en memoria contra MySQL (categoría `Integracion`) | Sí |
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
| Sin migraciones de EF | El esquema vive en un solo script SQL que todo el equipo entiende |
| `IDateTimeProvider` | Permite probar el semáforo de caducidad con fechas fijas |
