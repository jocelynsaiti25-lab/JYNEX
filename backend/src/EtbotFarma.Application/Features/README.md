# Features (vertical slices)

Cada historia de usuario es una carpeta con todo lo que necesita:

```
Features/
└─ <Modulo>/
   └─ <CasoDeUso>/
      ├─ <CasoDeUso>Command.cs   (o Query.cs)
      ├─ <CasoDeUso>Handler.cs
      ├─ <CasoDeUso>Validator.cs (si recibe datos: un if por regla)
      └─ <Resultado>Dto.cs
```

| Módulo        | Historias                          |
|---------------|------------------------------------|
| Auth          | HU-01, HU-02                              |
| Usuarios      | HU-03, HU-04, HU-05                       |
| Medicamentos  | HU-06, HU-09, HU-11                       |
| Inventario    | HU-07, HU-08, HU-10, HU-12, HU-13, HU-14  |
| Bitacora      | HU-15, HU-16                              |

## Ejemplos para aprender

- `Sistema/ObtenerEstado`: ejemplo de **Query** (solo lee).
- `Ejemplo/`: ejemplo de **Command con validador**:
  - `Producto.cs`: los datos que llegan.
  - `ProductoValidator.cs`: las reglas, un `if` por cada una; devuelve la lista de errores.
  - `ProductoHandler.cs`: llama al validador; si hay errores lanza `ValidacionException` (HTTP 400),
    si no, ejecuta la lógica.

Pruébalo con `EtbotFarma.Api.http` (una petición correcta y una incorrecta).
La carpeta `Ejemplo/` se puede borrar cuando ya no sirva de guía.
