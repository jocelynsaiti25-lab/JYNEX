# Arquitectura

**Clean Architecture + CQRS organizada por funcionalidad (vertical slices)**, en un monolito modular.

## Capas y regla de dependencias

```
Angular  →  Api  →  Application  →  Domain
                        ↑
                  Infrastructure
```

| Capa | Responsabilidad | Depende de |
|------|-----------------|------------|
| **Domain** | Entidades y reglas de negocio (semáforo, FEFO, agotado) | Nada |
| **Application** | Casos de uso: commands, queries, handlers, validadores (clases propias) e interfaces | Domain |
| **Infrastructure** | Implementaciones técnicas: EF Core, hash de contraseñas, Excel | Application |
| **Api** | Controladores HTTP delgados, manejo global de errores | Application, Infrastructure |

Regla: **las capas internas nunca conocen a las externas.** Infrastructure implementa interfaces
definidas en Application (principio de inversión de dependencias).

## Flujo de una petición

1. Angular llama `POST /api/sesiones/iniciar` (HU-01).
2. `InicioSesionController` envía `IniciarSesionCommand` con MediatR.
3. `ValidationBehavior` (pipeline de MediatR) ejecuta `IniciarSesionValidator`.
   Si hay errores lanza `ValidacionException` → HTTP 400 y el handler NO se ejecuta.
4. Si no hay errores, `IniciarSesionHandler` coordina el caso de uso y devuelve el resultado.
5. Si se lanza `DomainException` → HTTP 422. Cualquier otro error → HTTP 500.

## Decisiones

| Decisión | Motivo |
|----------|--------|
| MediatR 12.5.0 | Última versión con licencia libre (Apache 2.0) |
| Validadores propios (`IValidator<T>`) + `ValidationBehavior` | Sin librerías: un `if` por regla; MediatR los ejecuta antes de cada handler |
| Carpetas por tipo y por historia | `Commands/`, `Validators/`, `Handlers/`, `Dtos/` con una subcarpeta por historia; un controller por historia para evitar conflictos en Git |
| Sesión por cookie (HU-01) | Los criterios exigen invalidar sesiones en el servidor (HU-02, HU-05) |
| Proxy de Angular a la API | Mismo origen en desarrollo: las cookies funcionan sin configurar CORS |
| `IDateTimeProvider` | Permite probar el semáforo de caducidad con fechas fijas |
