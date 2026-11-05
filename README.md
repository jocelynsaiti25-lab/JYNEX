# ETBOTFARMA WEB

Sistema web de inventario de medicamentos: control de lotes, semáforo de caducidad,
salidas FEFO, bitácora de movimientos y exportación a Excel.

## Tecnologías

| Parte | Tecnología |
|-------|------------|
| Frontend | Angular 20 (componentes standalone, signals) |
| Backend | .NET 10 / ASP.NET Core (C#) |
| Arquitectura | Clean Architecture + CQRS (MediatR) + vertical slices |
| Validaciones | FluentValidation |
| Base de datos | Por definir (EF Core, migraciones) |
| Pruebas | xUnit |

Detalles en [`docs/arquitectura.md`](docs/arquitectura.md).

## Estructura

```
etbotfarma-web/
├─ backend/
│  ├─ EtbotFarma.slnx                 Solución (.NET)
│  ├─ src/
│  │  ├─ EtbotFarma.Domain/           Entidades y reglas de negocio
│  │  ├─ EtbotFarma.Application/      Casos de uso (CQRS) y validadores
│  │  │  ├─ Common/                   Behaviors, interfaces, ICommand/IQuery
│  │  │  └─ Features/                 Una carpeta por módulo / historia
│  │  ├─ EtbotFarma.Infrastructure/   BD, servicios técnicos
│  │  └─ EtbotFarma.Api/              Controladores y manejo de errores
│  └─ tests/                          Pruebas unitarias
├─ frontend/                          Angular
│  └─ src/app/
│     ├─ core/                        Servicios, interceptores, guards, modelos
│     ├─ shared/                      Componentes reutilizables
│     └─ features/                    Pantallas por módulo
├─ database/                          Scripts de BD
├─ docs/                              Arquitectura e historias de usuario
└─ .github/workflows/ci.yml           Compilación y pruebas automáticas
```

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 22 LTS](https://nodejs.org/)
- Angular CLI: `npm install -g @angular/cli@20`
- Visual Studio 2022 (17.14+) / Visual Studio 2026, o VS Code

## Cómo ejecutar

**1. Backend** (terminal 1)

```bash
cd backend
dotnet restore EtbotFarma.slnx
dotnet build EtbotFarma.slnx
dotnet test EtbotFarma.slnx
dotnet run --project src/EtbotFarma.Api
```

La API queda en `http://localhost:5080`. Prueba: `http://localhost:5080/api/sistema/estado`

**2. Frontend** (terminal 2)

```bash
cd frontend
npm install
npm start
```

Abre `http://localhost:4200`. Si ves "API conectada", todo funciona.

## Cómo agregar una historia de usuario

1. Crear la carpeta `backend/src/EtbotFarma.Application/Features/<Modulo>/<CasoDeUso>/`.
2. Agregar `Command` o `Query`, `Handler`, `Validator` y `Dto` (copiar el ejemplo `Sistema/ObtenerEstado`).
3. Las reglas de negocio van en las entidades de `Domain`.
4. Exponer el endpoint en un controlador de `Api/Controllers` (solo llama `Sender.Send(...)`).
5. Crear la pantalla en `frontend/src/app/features/<modulo>/`.
6. Escribir pruebas en `backend/tests/`.

## Flujo de trabajo en Git

- `main`: versión estable.
- `develop`: integración.
- `feature/HU-01-inicio-sesion`: una rama por historia, con Pull Request hacia `develop`.

Mensajes de commit sugeridos: `feat(HU-01): ...`, `fix(HU-02): ...`, `docs: ...`, `test: ...`.
