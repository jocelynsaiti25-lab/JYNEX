# Application — organización por tipo y por historia

Cada **tipo** de archivo tiene su carpeta, y dentro hay una **subcarpeta por historia de usuario**
(con el nombre de la historia, sin el número):

```
EtbotFarma.Application/
├─ Common/
│  ├─ Behaviors/ValidationBehavior.cs   ← MediatR ejecuta los validators (FluentValidation) antes de cada handler
│  ├─ Interfaces/                       ← repositorios, hash, token, cookie, usuario actual, unidad de trabajo, reloj
│  ├─ Exceptions/  Messaging/  Configuracion/
├─ Commands/<Historia>/                 ← operaciones que MODIFICAN datos
├─ Queries/<Historia>/                  ← operaciones que solo LEEN
├─ Validators/<Historia>/               ← AbstractValidator<T> (se ejecutan solos con MediatR)
├─ Handlers/<Historia>/                 ← solo coordinan los pasos
└─ Dtos/<Historia>/                     ← datos que devuelve cada caso de uso
```

Ejemplo real (HU-01, carpeta `InicioSesion`):

```
Commands/InicioSesion/IniciarSesionCommand.cs       ValidarSesionCommand.cs
Queries/InicioSesion/ObtenerSesionActualQuery.cs
Validators/InicioSesion/IniciarSesionValidator.cs
Handlers/InicioSesion/IniciarSesionHandler.cs       ValidarSesionHandler.cs   ObtenerSesionActualHandler.cs
Dtos/InicioSesion/UsuarioSesionDto.cs
```

## Nombre de carpeta de cada historia

| HU | Carpeta | Controller (Api) | Angular (`features/`) |
|----|---------|------------------|------------------------|
| 01 | `InicioSesion` | `InicioSesionController` | `inicio-sesion` |
| 02 | `CierreSesion` | `CierreSesionController` | `cierre-sesion` |
| 03 | `CreacionUsuarios` | `CreacionUsuariosController` | `creacion-usuarios` |
| 04 | `ConsultaUsuarios` | `ConsultaUsuariosController` | `consulta-usuarios` |
| 05 | `ReasignacionContrasena` | `ReasignacionContrasenaController` | `reasignacion-contrasena` |
| 06 | `RegistroMedicamentos` | `RegistroMedicamentosController` | `registro-medicamentos` |
| 07 | `EntradaLote` | `EntradaLoteController` | `entrada-lote` |
| 08 | `BusquedaMedicamentos` | `BusquedaMedicamentosController` | `busqueda-medicamentos` |
| 09 | `EdicionMedicamentos` | `EdicionMedicamentosController` | `edicion-medicamentos` |
| 10 | `SemaforoCaducidad` | `SemaforoCaducidadController` | `semaforo-caducidad` |
| 11 | `StockMinimo` | `StockMinimoController` | `stock-minimo` |
| 12 | `MedicamentosAgotados` | `MedicamentosAgotadosController` | `medicamentos-agotados` |
| 13 | `SalidaMedicamentos` | `SalidaMedicamentosController` | `salida-medicamentos` |
| 14 | `BajaLotes` | `BajaLotesController` | `baja-lotes` |
| 15 | `BitacoraMovimientos` | `BitacoraMovimientosController` | `bitacora-movimientos` |
| 16 | `ExportacionBitacora` | `ExportacionBitacoraController` | `exportacion-bitacora` |

`EstadoSistema` no es una historia: solo comprueba que la API responde.

## Cómo funciona la validación (FluentValidation + MediatR)

```
Controller → sender.Send(command)
               ↓
         ValidationBehavior   (recibe los IValidator<TCommand> y ejecuta ValidateAsync)
               ↓  ¿errores? Sí → ValidacionException → HTTP 400 (el handler NO se ejecuta)
               ↓            No
            Handler           (sin validaciones)
```

Para validar un command nuevo solo hay que crear la clase en `Validators/<Historia>/`
(ejemplo real: `Validators/InicioSesion/IniciarSesionValidator.cs`):

```csharp
public sealed class IniciarSesionValidator : AbstractValidator<IniciarSesionCommand>
{
    public IniciarSesionValidator()
    {
        RuleFor(command => command.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

        RuleFor(command => command.Contrasenia)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
```

Se registra solo (`services.AddValidatorsFromAssembly` en `DependencyInjection.cs`); no se llama desde el handler.
Explicación completa: `docs/arquitectura.md`.
