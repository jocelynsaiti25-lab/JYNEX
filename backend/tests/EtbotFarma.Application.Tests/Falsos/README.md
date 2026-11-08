# Falsos (implementaciones de prueba)

Implementaciones **falsas** de las interfaces de Application, solo para pruebas.
Gracias a que los handlers dependen de interfaces (principio **D** de SOLID: inversión de dependencias),
se pueden probar sin base de datos, sin cookies y con una fecha fija.

## Regla: un archivo por clase falsa

| Interfaz (Application) | Falso |
|---|---|
| `IUsuarioRepository` | `UsuarioRepositoryFalso.cs` |
| `ISesionRepository` | `SesionRepositoryFalso.cs` |
| `IPasswordHasher` | `PasswordHasherFalso.cs` |
| `ITokenSesionService` | `TokenSesionServiceFalso.cs` |
| `IUnitOfWork` | `UnitOfWorkFalso.cs` |
| `IDateTimeProvider` | `RelojFalso.cs` |
| `ICookieSesionService` | `CookieSesionServiceFalso.cs` |
| `IUsuarioActual` | `UsuarioActualFalso.cs` |

## Cómo agregar el falso de tu historia

1. Crea **tu propio archivo** `<Nombre>Falso.cs` en esta carpeta, con el namespace `EtbotFarma.Application.Tests.Falsos`.
   Ejemplo: `IMedicamentoRepository` → `MedicamentoRepositoryFalso.cs`.
2. Guarda los datos en listas en memoria (`List<T>`) y, si te sirve para las pruebas, cuenta las llamadas
   (por ejemplo `VecesGuardado`).
3. Agrega una fila a la tabla de arriba.

Si tu historia agrega un método nuevo a una interfaz que ya existe (por ejemplo `ActualizarAsync` en
`IUsuarioRepository`), agrega ese método **solo** en el falso correspondiente. No cambies los métodos
que ya usan otras historias.

¿Por qué un archivo por clase? Antes todos los falsos estaban en un solo `Falsos.cs`.
Con 16 historias editándolo a la vez, cada Pull Request habría tenido conflictos de Git en ese archivo.
