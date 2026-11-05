# Historias de usuario

Fuente: `Historias_Usuario.xlsx` (Google Drive del equipo).

| ID | Historia | Módulo | Tipo CQRS | Estado |
|----|----------|--------|-----------|--------|
| HU-01 | Autenticación e inicio de sesión | Auth | Command | Pendiente |
| HU-02 | Cierre seguro de sesión | Auth | Command | Pendiente |
| HU-03 | Creación de usuarios | Usuarios | Command | Pendiente |
| HU-04 | Consulta de usuarios registrados | Usuarios | Query | Pendiente |
| HU-05 | Reasignación de contraseña | Usuarios | Command | Pendiente |
| HU-06 | Registro de medicamentos | Medicamentos | Command | Pendiente |
| HU-07 | Búsqueda de medicamentos | Inventario | Query | Pendiente |
| HU-08 | Edición de información de medicamentos | Medicamentos | Command | Pendiente |
| HU-09 | Semáforo y filtros de caducidad | Inventario | Query | Pendiente |
| HU-10 | Bitácora de movimientos | Bitacora | Query | Pendiente |
| HU-11 | Registro de salida (FEFO) | Inventario | Query + Command | Pendiente |
| HU-12 | Baja de lotes en estado rojo | Inventario | Command | Pendiente |
| HU-13 | Control de stock mínimo | Medicamentos | Command | Pendiente |
| HU-14 | Consulta de medicamentos agotados | Inventario | Query | Pendiente |
| HU-15 | Exportación de bitácora a Excel | Bitacora | Query | Pendiente |

## Pendientes de corregir en el Excel

- La hoja de criterios usa una numeración distinta a la del índice.
- La historia FEFO solo aparece en la hoja de criterios.
- HU-04 tiene prioridad "Medios de comunicación" (debe decir "Media").
- Roles contradictorios en HU-10 y HU-12.
