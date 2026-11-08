# ETBOTFARMA WEB

Sistema web de inventario de medicamentos: control de lotes, semáforo de caducidad,
salidas FEFO, bitácora de movimientos y exportación a Excel.

## Tecnologías

| Parte | Tecnología |
|-------|------------|
| Frontend | Angular 20 (componentes standalone, signals) |
| Backend | .NET 10 / ASP.NET Core (C#) |
| Arquitectura | Clean Architecture + CQRS (MediatR), carpetas por tipo y por historia |
| Validaciones | FluentValidation 12.1.1, ejecutada automáticamente por MediatR |
| Base de datos | MySQL 8 con SQL a mano (MySqlConnector), sin ORM |
| Pruebas | xUnit (unitarias, de arquitectura y de integración), Playwright |

Detalles en [`docs/arquitectura.md`](docs/arquitectura.md).

## Estructura

```
etbotfarma-web/
├─ backend/
│  ├─ EtbotFarma.slnx                 Solución (.NET)
│  ├─ src/
│  │  ├─ EtbotFarma.Domain/           Entidades y reglas de negocio
│  │  ├─ EtbotFarma.Application/      Casos de uso (CQRS) y validadores
│  │  │  ├─ Common/                   ValidationBehavior, interfaces, excepciones
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

1. Crea la base en MySQL 8 con `database/etbotfarma_mysql_v2.sql` (si ya tenías la versión 1: `database/migracion_v1_a_v2.sql`). Ver `database/README.md`.
2. En `backend/src/EtbotFarma.Api/`, copia `appsettings.Local.example.json` como
   **`appsettings.Local.json`** y escribe tu contraseña de MySQL y la contraseña que tendrá el usuario `admin`.
   Ese archivo **no se sube a GitHub** (está en `.gitignore`).
3. Al arrancar la API por primera vez se crea el usuario `admin` con esa contraseña.

**1. Backend** (terminal 1)

```bash
cd backend
dotnet restore EtbotFarma.slnx
dotnet build EtbotFarma.slnx
dotnet test EtbotFarma.slnx --filter "Category!=Integracion"
dotnet run --project src/EtbotFarma.Api
```

> El filtro `Category!=Integracion` deja fuera las pruebas que necesitan MySQL.
> Si ya tienes MySQL con la base creada y `appsettings.Local.json`, puedes correr todas con `dotnet test EtbotFarma.slnx`.

La API queda en `http://localhost:5080`. Prueba: `http://localhost:5080/api/sistema/estado`

**2. Frontend** (terminal 2)

```bash
cd frontend
npm ci
npm start
```

> `npm ci` instala exactamente las versiones de `package-lock.json` (las mismas para todo el equipo).
> Usa `npm install <paquete>` solo si el equipo aprobó agregar una librería nueva.

Abre `http://localhost:4200` e inicia sesión con el usuario `admin` y la contraseña que pusiste en `appsettings.Local.json`.

## Ejecutar en el navegador (GitHub Codespaces, sin instalar nada)

1. En GitHub, en la rama `develop`: botón verde **Code → Codespaces → Create codespace on develop**.
2. Espera a que termine de prepararse (la primera vez tarda unos minutos: instala .NET, Node, MySQL y crea la base).
3. En la terminal escribe `bash .devcontainer/iniciar.sh`.
4. Se abre la app en otra pestaña. Usuario `admin`, contraseña `Prueba123!` (solo de desarrollo).
5. Al terminar: en github.com/codespaces detén el Codespace para no gastar horas gratis.

## Cómo agregar una historia de usuario

Antes de empezar lee la **guía de trabajo en equipo**: [`docs/trabajo-en-equipo.md`](docs/trabajo-en-equipo.md)
(ramas, archivos compartidos, conflictos y lista de revisión del Pull Request).
El estado de cada historia está en [`docs/historias-usuario.md`](docs/historias-usuario.md).

Las reglas completas están en [`CLAUDE.md`](CLAUDE.md). En resumen (copiar HU-01 como molde):

1. Buscar el nombre de carpeta de la historia en `backend/src/EtbotFarma.Application/README.md` (ej.: HU-02 → `CierreSesion`).
2. Crear en Application: `Commands/<Historia>/`, `Validators/<Historia>/`, `Handlers/<Historia>/`, `Dtos/<Historia>/`.
3. Las reglas de negocio van en las entidades de `Domain/Entities/`.
4. Crear `Api/Controllers/<Historia>Controller.cs` y `Api/Contracts/<Historia>/` (el controller recibe `ISender` por constructor y solo llama `sender.Send(...)`).
5. Crear la pantalla en `frontend/src/app/features/<historia-en-kebab>/`.
6. Escribir pruebas en `backend/tests/` (`Handlers/<Historia>/`, `Validators/<Historia>/`); los falsos nuevos van en su propio archivo en `Falsos/`.

## Flujo de trabajo en Git

- `main`: versión estable (se actualiza desde `develop` con un Pull Request cuando una fase está lista).
- `develop`: integración. **Aquí está el código más reciente: crea tus ramas desde aquí.**
- `feature/<historia>` (ej.: `feature/cierre-sesion`): una rama por historia, con Pull Request hacia `develop`.

Mensajes de commit sugeridos: `feat(HU-01): ...`, `fix(HU-02): ...`, `docs: ...`, `test: ...`.
