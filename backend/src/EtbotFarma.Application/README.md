# Features (vertical slices)

Cada historia de usuario es una carpeta dentro de su módulo, con el número de la historia:

```
Features/
└─ <Modulo>/
   └─ HU<NN>_<CasoDeUso>/
      ├─ <CasoDeUso>Command.cs   (o Query.cs)
      ├─ <CasoDeUso>Validator.cs (si recibe datos: un if por regla)
      ├─ <CasoDeUso>Handler.cs
      └─ <CasoDeUso>Dto.cs
```

Ejemplo real (HU-01):

```
Features/Sesiones/HU01_IniciarSesion/
   IniciarSesionCommand.cs
   IniciarSesionValidator.cs
   IniciarSesionHandler.cs
   IniciarSesionDto.cs
```

| Módulo        | Historias                                 |
|---------------|-------------------------------------------|
| Sesiones      | HU-01, HU-02                              |
| Usuarios      | HU-03, HU-04, HU-05                       |
| Medicamentos  | HU-06, HU-09, HU-11                       |
| Inventario    | HU-07, HU-08, HU-10, HU-12, HU-13, HU-14  |
| Bitacora      | HU-15, HU-16                              |

Para buscar todo lo de una historia en VS Code: **Ctrl + Shift + F** y escribe `HU01`.

`Sistema/ObtenerEstado` no es una historia: solo comprueba que la API responde.
