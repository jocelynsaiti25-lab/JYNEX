# ETBOTFARMA WEB

Sistema web de inventario de medicamentos: control de lotes, semáforo de caducidad,
salidas FEFO, bitácora de movimientos y exportación a Excel.

## Tecnologías

| Parte | Tecnología |
|-------|------------|
| Frontend | Angular 20 (componentes standalone, signals) |
| Backend | .NET 10 / ASP.NET Core (C#) |
| Arquitectura | Clean Architecture + CQRS (MediatR) + vertical slices |
| Validaciones | Clases `IValidator<T>` propias (un `if` por regla), ejecutadas por MediatR |
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
│  │  │  ├─ Common/                   ValidationBehavior, IValidator, interfaces
│  │  │  ├─ Commands/ Queries/        Por tipo, con una subcarpeta por historia
│  │  │  ├─ Validators/ Handlers/     (ej.: Handlers/InicioSesion/)
│  │  │  └─ Dtos/
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

**0. Base de datos y configuración local** (solo la primera vez)

1. Crea la base en MySQL 8 con `database/etbotfarma_mysql.sql` (ver `database/README.md`).
2. En `backend/src/EtbotFarma.Api/`, copia `appsettings.Local.example.json` como
   **`appsettings.Local.json`** y escribe tu contraseña de MySQL y la contraseña que tendrá el usuario `admin`.
   Ese archivo **no se sube a GitHub** (está en `.gitignore`).
3. Al arrancar la API por primera vez se crea el usuario `admin` con esa contraseña.

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

## Ejecutar en el navegador (GitHub Codespaces, sin instalar nada)

1. En GitHub, en la rama `develop`: botón verde **Code → Codespaces → Create codespace on develop**.
2. Espera a que termine de prepararse (la primera vez tarda unos minutos: instala .NET, Node, MySQL y crea la base).
3. En la terminal escribe `bash .devcontainer/iniciar.sh`.
4. Se abre la app en otra pestaña. Usuario `admin`, contraseña `Prueba123!` (solo de desarrollo).
5. Al terminar: en github.com/codespaces detén el Codespace para no gastar horas gratis.

## Cómo agregar una historia de usuario

Las reglas completas están en [`CLAUDE.md`](CLAUDE.md). En resumen (copiar HU-01 como molde):

1. Buscar el nombre de carpeta de la historia en `backend/src/EtbotFarma.Application/README.md` (ej.: HU-02 → `CierreSesion`).
2. Crear en Application: `Commands/<Historia>/`, `Validators/<Historia>/`, `Handlers/<Historia>/`, `Dtos/<Historia>/`.
3. Las reglas de negocio van en las entidades de `Domain/Entities/`.
4. Crear `Api/Controllers/<Historia>Controller.cs` y `Api/Contracts/<Historia>/` (el controller solo llama `Sender.Send(...)`).
5. Crear la pantalla en `frontend/src/app/features/<historia-en-kebab>/`.
6. Escribir pruebas en `backend/tests/` (`Handlers/<Historia>/`, `Validators/<Historia>/`).

## Flujo de trabajo en Git

- `main`: versión estable.
- `develop`: integración.
- `feature/<historia>` (ej.: `feature/cierre-sesion`): una rama por historia, con Pull Request hacia `develop`.

Mensajes de commit sugeridos: `feat(HU-01): ...`, `fix(HU-02): ...`, `docs: ...`, `test: ...`.
