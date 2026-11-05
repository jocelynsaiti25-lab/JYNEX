# Features (vertical slices)

Cada historia de usuario es una carpeta con todo lo que necesita:

```
Features/
└─ <Modulo>/
   └─ <CasoDeUso>/
      ├─ <CasoDeUso>Command.cs   (o Query.cs)
      ├─ <CasoDeUso>Handler.cs
      ├─ <CasoDeUso>Validator.cs (FluentValidation, si recibe datos)
      └─ <Resultado>Dto.cs
```

| Módulo        | Historias                          |
|---------------|------------------------------------|
| Auth          | HU-01, HU-02                       |
| Usuarios      | HU-03, HU-04, HU-05                |
| Medicamentos  | HU-06, HU-08, HU-13                |
| Inventario    | HU-07, HU-09, HU-11, HU-12, HU-14  |
| Bitacora      | HU-10, HU-15                       |

`Sistema/ObtenerEstado` es el ejemplo base que hay que copiar.
